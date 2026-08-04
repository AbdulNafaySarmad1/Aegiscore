using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface ILabBookingRepository : IRepository<LabBooking>
{
    Task<IReadOnlyList<LabBooking>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabBooking>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabBooking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabBooking>> GetPendingApprovalsAsync(Guid supervisorId, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid labId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}
