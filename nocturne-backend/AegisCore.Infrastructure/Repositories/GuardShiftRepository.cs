using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class GuardShiftRepository : Repository<GuardShift>, IGuardShiftRepository
{
    public GuardShiftRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<GuardShift>> GetByGuardAsync(Guid guardId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.SecurityGuardId == guardId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GuardShift>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.ShiftDate.Date == date.Date).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GuardShift>> GetByGateAsync(string gate, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.Gate == gate && s.ShiftDate.Date == date.Date).ToListAsync(cancellationToken);
    }
}
