using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class VisitorRepository : Repository<Visitor>, IVisitorRepository
{
    public VisitorRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Visitor>> GetByHostEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.HostEmployeeId == employeeId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Visitor>> GetByHostStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.HostStudentId == studentId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Visitor>> GetCheckedInAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.Status == VisitorStatus.CheckedIn).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Visitor>> GetBlacklistedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.IsBlacklisted).ToListAsync(cancellationToken);
    }

    public async Task<Visitor?> GetByCNICAsync(string cnic, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(v => v.CNIC == cnic, cancellationToken);
    }
}
