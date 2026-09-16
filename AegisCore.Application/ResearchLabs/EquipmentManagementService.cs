using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.ResearchLabs;

public class EquipmentManagementService
{
    private readonly ILabEquipmentRepository _equipmentRepository;
    private readonly IEquipmentBookingRepository _bookingRepository;
    private readonly IEquipmentMaintenanceRepository _maintenanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EquipmentManagementService(
        ILabEquipmentRepository equipmentRepository,
        IEquipmentBookingRepository bookingRepository,
        IEquipmentMaintenanceRepository maintenanceRepository,
        IUnitOfWork unitOfWork)
    {
        _equipmentRepository = equipmentRepository;
        _bookingRepository = bookingRepository;
        _maintenanceRepository = maintenanceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabEquipment> CreateEquipmentAsync(LabEquipment equipment, CancellationToken cancellationToken = default)
    {
        var created = await _equipmentRepository.AddAsync(equipment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<EquipmentBooking> BookEquipmentAsync(EquipmentBooking booking, CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(booking.LabEquipmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Equipment not found.");

        if (equipment.Status != EquipmentStatus.Available)
            throw new InvalidOperationException("Equipment is not available.");

        var hasConflict = await _bookingRepository.HasConflictAsync(
            booking.LabEquipmentId, booking.StartTime, booking.EndTime, cancellationToken);

        if (hasConflict)
            throw new InvalidOperationException("Time slot conflicts with an existing booking.");

        booking.Status = BookingStatus.Pending;
        var created = await _bookingRepository.AddAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<EquipmentMaintenance> ReportMaintenanceAsync(EquipmentMaintenance maintenance, CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(maintenance.LabEquipmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Equipment not found.");

        equipment.Status = EquipmentStatus.UnderMaintenance;
        maintenance.Status = MaintenanceStatus.Reported;

        await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
        var created = await _maintenanceRepository.AddAsync(maintenance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task CompleteMaintenanceAsync(Guid maintenanceId, string resolutionNotes, decimal cost, CancellationToken cancellationToken = default)
    {
        var maintenance = await _maintenanceRepository.GetByIdAsync(maintenanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Maintenance request not found.");

        var equipment = await _equipmentRepository.GetByIdAsync(maintenance.LabEquipmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Equipment not found.");

        maintenance.Status = MaintenanceStatus.Completed;
        maintenance.CompletedAt = DateTime.UtcNow;
        maintenance.ResolutionNotes = resolutionNotes;
        maintenance.Cost = cost;
        maintenance.UpdatedAt = DateTime.UtcNow;

        equipment.Status = EquipmentStatus.Available;
        equipment.UpdatedAt = DateTime.UtcNow;

        await _maintenanceRepository.UpdateAsync(maintenance, cancellationToken);
        await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
