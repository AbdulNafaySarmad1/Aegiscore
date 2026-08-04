using Microsoft.EntityFrameworkCore;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.Repositories;

public class AttendanceRepository : Repository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<double> GetAttendancePercentageAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
    {
        var attendances = await _dbSet
            .Where(a => a.EnrollmentId == enrollmentId)
            .ToListAsync(cancellationToken);

        if (attendances.Count == 0)
            return 0;

        var presentCount = attendances.Count(a => a.Status == Domain.AttendanceStatus.Present);
        return (double)presentCount / attendances.Count * 100;
    }

    public async Task<IReadOnlyList<Attendance>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Date >= startDate && a.Date <= endDate)
            .Include(a => a.Enrollment)
                .ThenInclude(e => e.Student)
            .Include(a => a.Course)
            .ToListAsync(cancellationToken);
    }
}
