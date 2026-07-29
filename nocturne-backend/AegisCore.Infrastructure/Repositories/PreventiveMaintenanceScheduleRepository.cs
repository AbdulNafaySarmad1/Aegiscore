using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class PreventiveMaintenanceScheduleRepository : Repository<PreventiveMaintenanceSchedule>, IPreventiveMaintenanceScheduleRepository
{
    public PreventiveMaintenanceScheduleRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<PreventiveMaintenanceSchedule>> GetByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.AssetId == assetId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PreventiveMaintenanceSchedule>> GetDueAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.IsActive && s.NextDueAt.HasValue && s.NextDueAt <= DateTime.UtcNow).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PreventiveMaintenanceSchedule>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.IsActive && s.NextDueAt.HasValue && s.NextDueAt < DateTime.UtcNow).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PreventiveMaintenanceSchedule>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.IsActive).ToListAsync(cancellationToken);
    }
}
