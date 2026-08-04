using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface ILoginAuditRecordRepository : IRepository<LoginAuditRecord>
{
    Task<IReadOnlyList<LoginAuditRecord>> GetByUserAsync(Guid userId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoginAuditRecord>> GetByIpAsync(string ipAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoginAuditRecord>> GetFailedAsync(Guid? tenantId = null, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoginAuditRecord>> GetByEventTypeAsync(LoginEvent eventType, DateTime? from = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoginAuditRecord>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
}
