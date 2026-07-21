using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByUniversityIdAsync(string universityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetByBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
}
