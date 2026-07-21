using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IConfigurationEntryRepository : IRepository<ConfigurationEntry>
{
    Task<ConfigurationEntry?> GetByKeyAsync(string key, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigurationEntry>> GetByDomainAsync(ConfigurationDomain domain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigurationEntry>> GetByEnvironmentAsync(ConfigEnvironment environment, CancellationToken cancellationToken = default);
}

public interface IConfigVersionRepository : IRepository<ConfigVersion>
{
    Task<IReadOnlyList<ConfigVersion>> GetByEntryAsync(Guid entryId, CancellationToken cancellationToken = default);
    Task<ConfigVersion?> GetByEntryAndVersionAsync(Guid entryId, int versionNumber, CancellationToken cancellationToken = default);
}

public interface IConfigAuditLogRepository : IRepository<ConfigAuditLog>
{
    Task<IReadOnlyList<ConfigAuditLog>> GetByEntryAsync(Guid entryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigAuditLog>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigAuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
