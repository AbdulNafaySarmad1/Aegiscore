using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IAttendanceRepository : IRepository<Attendance>
{
    Task<double> GetAttendancePercentageAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attendance>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
