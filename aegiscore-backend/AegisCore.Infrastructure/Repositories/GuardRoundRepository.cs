using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class GuardRoundRepository : Repository<GuardRound>, IGuardRoundRepository
{
    public GuardRoundRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<GuardRound>> GetByGuardAsync(Guid guardId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.SecurityGuardId == guardId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GuardRound>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.RoundStartTime.Date == date.Date).ToListAsync(cancellationToken);
    }

    public async Task<GuardRound?> GetWithScansAsync(Guid roundId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.CheckpointScans)
            .FirstOrDefaultAsync(r => r.Id == roundId, cancellationToken);
    }
}
