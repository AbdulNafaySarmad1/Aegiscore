using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IVisitorRepository : IRepository<Visitor>
{
    Task<IReadOnlyList<Visitor>> GetByHostEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Visitor>> GetByHostStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Visitor>> GetCheckedInAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Visitor>> GetBlacklistedAsync(CancellationToken cancellationToken = default);
    Task<Visitor?> GetByCNICAsync(string cnic, CancellationToken cancellationToken = default);
}
