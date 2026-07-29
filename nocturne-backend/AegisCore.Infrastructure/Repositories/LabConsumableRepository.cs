using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class LabConsumableRepository : Repository<LabConsumable>, ILabConsumableRepository
{
    public LabConsumableRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LabConsumable>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.ResearchLabId == labId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LabConsumable>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.ResearchLab)
            .Where(c => c.QuantityInStock <= c.MinimumStockLevel)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LabConsumable>> GetExpiringAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.ResearchLab)
            .Where(c => c.ExpiryDate.HasValue && c.ExpiryDate <= beforeDate)
            .ToListAsync(cancellationToken);
    }
}
