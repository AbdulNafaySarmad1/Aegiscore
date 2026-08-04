using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IStudentRepository : IRepository<Student>
{
    Task<Student?> GetByUniversityIdAsync(string universityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Student>> GetByBatchAsync(Guid batchId, CancellationToken cancellationToken = default);
}
