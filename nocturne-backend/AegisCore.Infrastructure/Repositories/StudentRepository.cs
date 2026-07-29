using Microsoft.EntityFrameworkCore;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.Repositories;

public class StudentRepository : Repository<Student>, IStudentRepository
{
    public StudentRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<Student?> GetByUniversityIdAsync(string universityId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Batch)
            .FirstOrDefaultAsync(s => s.UniversityId == universityId, cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetByBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.BatchId == batchId)
            .Include(s => s.Batch)
            .ToListAsync(cancellationToken);
    }
}
