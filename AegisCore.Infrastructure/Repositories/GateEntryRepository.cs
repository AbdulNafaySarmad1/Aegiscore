using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class GateEntryRepository : Repository<GateEntry>, IGateEntryRepository
{
    public GateEntryRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<GateEntry>> GetByVehiclePassAsync(Guid vehiclePassId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(g => g.VehiclePassId == vehiclePassId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GateEntry>> GetByVisitorAsync(Guid visitorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(g => g.VisitorId == visitorId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GateEntry>> GetByGateAsync(string gateName, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(g => g.GateName == gateName && g.EntryTime.Date == date.Date).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GateEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(g => g.EntryTime >= startDate && g.EntryTime <= endDate).ToListAsync(cancellationToken);
    }
}
