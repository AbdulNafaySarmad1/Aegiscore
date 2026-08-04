using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class SecurityGuardRepository : Repository<SecurityGuard>, ISecurityGuardRepository
{
    public SecurityGuardRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SecurityGuard>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(g => g.Status == GuardStatus.Active).ToListAsync(cancellationToken);
    }

    public async Task<SecurityGuard?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(g => g.EmployeeId == employeeId, cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityGuard>> GetByPostAsync(string post, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(g => g.Shifts)
            .Where(g => g.Shifts.Any(s => s.Post == post && s.Status == ShiftStatus.InProgress))
            .ToListAsync(cancellationToken);
    }
}
