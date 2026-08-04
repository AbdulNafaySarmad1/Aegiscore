using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class LostFoundItemRepository : Repository<LostFoundItem>, ILostFoundItemRepository
{
    public LostFoundItemRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LostFoundItem>> GetByStatusAsync(LostFoundStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LostFoundItem>> GetByTypeAsync(LostFoundType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.Type == type).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LostFoundItem>> GetUnclaimedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.Status == LostFoundStatus.Unclaimed).ToListAsync(cancellationToken);
    }
}
