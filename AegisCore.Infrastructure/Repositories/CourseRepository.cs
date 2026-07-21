using Microsoft.EntityFrameworkCore;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.Repositories;

public class CourseRepository : Repository<Course>, ICourseRepository
{
    public CourseRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<Course?> GetWithPrerequisitesAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.PrerequisiteCourse)
            .Include(c => c.Department)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
    }

    public async Task<IReadOnlyList<Course>> GetBySemesterAsync(int semester, CancellationToken cancellationToken = default)
    {
        // Assuming semester is derived from credit hours or other logic
        // For now, return all courses as placeholder
        return await _dbSet
            .Include(c => c.Department)
            .ToListAsync(cancellationToken);
    }
}
