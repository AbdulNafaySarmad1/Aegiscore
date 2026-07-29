using AegisCore.Domain.Entities;

namespace AegisCore.Application.ConfigCenter;

public interface IConfigurationCenterService
{
    Task<ConfigurationEntry> SetConfigAsync(string key, string valueJson, Guid userId, string? userName, ConfigurationDomain domain, string? description, CancellationToken cancellationToken = default);
    Task<ConfigDto> GetConfigAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigDto>> GetDomainConfigsAsync(ConfigurationDomain domain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigVersionDto>> GetVersionHistoryAsync(Guid entryId, CancellationToken cancellationToken = default);
    Task<ConfigurationEntry> RollbackAsync(Guid entryId, int targetVersion, Guid userId, string? userName, CancellationToken cancellationToken = default);
    Task<ConfigDiffDto> GetDiffAsync(Guid entryId, int versionA, int versionB, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigAuditLogDto>> GetAuditLogAsync(Guid? entryId = null, Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
