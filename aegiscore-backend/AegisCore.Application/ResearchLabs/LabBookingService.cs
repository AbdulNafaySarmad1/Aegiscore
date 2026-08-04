using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.ResearchLabs;

public class LabBookingService
{
    private readonly IResearchLabRepository _labRepository;
    private readonly ILabBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LabBookingService(
        IResearchLabRepository labRepository,
        ILabBookingRepository bookingRepository,
        IUnitOfWork unitOfWork)
    {
        _labRepository = labRepository;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabBooking> CreateBookingAsync(LabBooking booking, CancellationToken cancellationToken = default)
    {
        var lab = await _labRepository.GetByIdAsync(booking.ResearchLabId, cancellationToken)
            ?? throw new InvalidOperationException("Lab not found.");

        if (lab.Status != LabStatus.Active)
            throw new InvalidOperationException("Lab is not active.");

        var hasConflict = await _bookingRepository.HasConflictAsync(
            booking.ResearchLabId, booking.StartTime, booking.EndTime, cancellationToken);

        if (hasConflict)
            throw new InvalidOperationException("Time slot conflicts with an existing booking.");

        booking.Status = BookingStatus.Pending;
        var created = await _bookingRepository.AddAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task ApproveBookingAsync(Guid bookingId, Guid approvedByUserId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new InvalidOperationException("Booking not found.");

        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only pending bookings can be approved.");

        booking.Status = BookingStatus.Approved;
        booking.ApprovedByUserId = approvedByUserId;
        booking.ApprovedAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new InvalidOperationException("Booking not found.");

        booking.Status = BookingStatus.Rejected;
        booking.RejectionReason = reason;
        booking.UpdatedAt = DateTime.UtcNow;

        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<LabCalendarDto> GetLabCalendarAsync(Guid labId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var bookings = await _bookingRepository.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        var labBookings = bookings.Where(b => b.ResearchLabId == labId).ToList();

        return new LabCalendarDto
        {
            LabId = labId,
            StartDate = startDate,
            EndDate = endDate,
            Bookings = labBookings.Select(b => new BookingSlotDto
            {
                Id = b.Id,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Purpose = b.Purpose,
                Status = b.Status,
                BookedByUserId = b.BookedByUserId
            }).ToList()
        };
    }
}

public class LabCalendarDto
{
    public Guid LabId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<BookingSlotDto> Bookings { get; set; } = new();
}

public class BookingSlotDto
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public BookingStatus Status { get; set; }
    public Guid BookedByUserId { get; set; }
}
