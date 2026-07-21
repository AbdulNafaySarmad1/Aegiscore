using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class LabBookingRepository : Repository<LabBooking>, ILabBookingRepository
{
    public LabBookingRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LabBooking>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.ResearchLabId == labId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LabBooking>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.BookedByUserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LabBooking>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.StartTime >= startDate && b.EndTime <= endDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LabBooking>> GetPendingApprovalsAsync(Guid supervisorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.ResearchLab)
            .Where(b => b.Status == BookingStatus.Pending && b.ResearchLab.LabSupervisorId == supervisorId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasConflictAsync(Guid labId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(b =>
            b.ResearchLabId == labId &&
            b.Status == BookingStatus.Approved &&
            b.StartTime < endTime &&
            b.EndTime > startTime, cancellationToken);
    }
}
