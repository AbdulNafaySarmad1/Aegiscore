using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    Task<Course?> GetWithPrerequisitesAsync(Guid courseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Course>> GetBySemesterAsync(int semester, CancellationToken cancellationToken = default);
}
