using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class EquipmentBookingRepository : Repository<EquipmentBooking>, IEquipmentBookingRepository
{
    public EquipmentBookingRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<EquipmentBooking>> GetByEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.LabEquipmentId == equipmentId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EquipmentBooking>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.BookedByUserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<bool> HasConflictAsync(Guid equipmentId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(b =>
            b.LabEquipmentId == equipmentId &&
            b.Status == BookingStatus.Approved &&
            b.StartTime < endTime &&
            b.EndTime > startTime, cancellationToken);
    }
}
