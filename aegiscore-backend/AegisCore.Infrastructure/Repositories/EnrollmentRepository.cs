using Microsoft.EntityFrameworkCore;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.Repositories;

public class EnrollmentRepository : Repository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Enrollment>> GetStudentEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Course)
            .Include(e => e.Student)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CheckEnrollmentExistsAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId, cancellationToken);
    }
}
