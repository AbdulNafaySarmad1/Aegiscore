using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<IReadOnlyList<Enrollment>> GetStudentEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<bool> CheckEnrollmentExistsAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
}
