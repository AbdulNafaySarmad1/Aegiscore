using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<IReadOnlyList<Enrollment>> GetStudentEnrollmentsAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<bool> CheckEnrollmentExistsAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
}
