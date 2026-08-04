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
        if (semester < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(semester), "Semester must be greater than zero.");
        }

        var courseIds = await _context.CurriculumCourses
            .Where(c => c.Semester == semester)
            .Select(c => c.CourseId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _dbSet
            .Include(c => c.Department)
            .Where(c => courseIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }
}
