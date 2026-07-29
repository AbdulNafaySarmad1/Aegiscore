using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface ISecurityGuardRepository : IRepository<SecurityGuard>
{
    Task<IReadOnlyList<SecurityGuard>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<SecurityGuard?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityGuard>> GetByPostAsync(string post, CancellationToken cancellationToken = default);
}
