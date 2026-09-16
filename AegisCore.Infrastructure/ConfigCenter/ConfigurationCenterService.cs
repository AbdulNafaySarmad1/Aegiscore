using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.ConfigCenter;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.ConfigCenter;

public class ConfigurationCenterService : IConfigurationCenterService
{
    private readonly IConfigurationEntryRepository _entryRepo;
    private readonly IConfigVersionRepository _versionRepo;
    private readonly IConfigAuditLogRepository _auditRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigurationCenterService> _logger;

    public ConfigurationCenterService(
        IConfigurationEntryRepository entryRepo,
        IConfigVersionRepository versionRepo,
        IConfigAuditLogRepository auditRepo,
        IMemoryCache cache,
        ILogger<ConfigurationCenterService> logger)
    {
        _entryRepo = entryRepo;
        _versionRepo = versionRepo;
        _auditRepo = auditRepo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ConfigurationEntry> SetConfigAsync(string key, string valueJson, Guid userId, string? userName, ConfigurationDomain domain, string? description, CancellationToken cancellationToken = default)
    {
        var existing = await _entryRepo.GetByKeyAsync(key, cancellationToken: cancellationToken);

        if (existing != null)
        {
            var previousValue = existing.ValueJson;
            var newVersion = existing.CurrentVersion + 1;

            var version = new ConfigVersion
            {
                Id = Guid.NewGuid(), ConfigurationEntryId = existing.Id,
                VersionNumber = newVersion, ValueJson = valueJson,
                ChangeDescription = description, ChangedByUserId = userId, ChangedByName = userName
            };

            existing.ValueJson = valueJson;
            existing.CurrentVersion = newVersion;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedByUserId = userId;
            existing.Description = description;

            await _versionRepo.AddAsync(version, cancellationToken);
            await _entryRepo.UpdateAsync(existing, cancellationToken);

            await LogAuditAsync(existing.Id, key, "Update", previousValue, valueJson, userId, userName, existing.CurrentVersion - 1, newVersion, cancellationToken);

            _cache.Remove($"config:{key}");
            _logger.LogInformation("Config updated: {Key} v{Version}", key, newVersion);

            return existing;
        }

        var entry = new ConfigurationEntry
        {
            Id = Guid.NewGuid(), Key = key, ValueJson = valueJson,
            Domain = domain, Description = description, CurrentVersion = 1,
            Status = ConfigEntryStatus.Active, Environment = ConfigEnvironment.Production
        };

        await _entryRepo.AddAsync(entry, cancellationToken);

        var initialVersion = new ConfigVersion
        {
            Id = Guid.NewGuid(), ConfigurationEntryId = entry.Id,
            VersionNumber = 1, ValueJson = valueJson,
            ChangeDescription = "Initial configuration.", ChangedByUserId = userId, ChangedByName = userName
        };

        await _versionRepo.AddAsync(initialVersion, cancellationToken);
        await LogAuditAsync(entry.Id, key, "Create", null, valueJson, userId, userName, null, 1, cancellationToken);

        _logger.LogInformation("Config created: {Key}", key);
        return entry;
    }

    public async Task<ConfigDto> GetConfigAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<ConfigDto>($"config:{key}", out var cached) && cached is not null)
            return cached;

        var entry = await _entryRepo.GetByKeyAsync(key, cancellationToken: cancellationToken);
        if (entry == null)
            throw new KeyNotFoundException($"Configuration '{key}' not found.");

        var dto = MapToDto(entry);
        _cache.Set($"config:{key}", dto, TimeSpan.FromMinutes(5));

        return dto;
    }

    public async Task<IReadOnlyList<ConfigDto>> GetDomainConfigsAsync(ConfigurationDomain domain, CancellationToken cancellationToken = default)
    {
        var entries = await _entryRepo.GetByDomainAsync(domain, cancellationToken);
        return entries.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ConfigVersionDto>> GetVersionHistoryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var versions = await _versionRepo.GetByEntryAsync(entryId, cancellationToken);
        return versions.Select(v => new ConfigVersionDto
        {
            Id = v.Id, VersionNumber = v.VersionNumber,
            Value = JsonSerializer.Deserialize<object>(v.ValueJson),
            ChangeDescription = v.ChangeDescription, ChangedByName = v.ChangedByName, CreatedAt = v.CreatedAt
        }).ToList();
    }

    public async Task<ConfigurationEntry> RollbackAsync(Guid entryId, int targetVersion, Guid userId, string? userName, CancellationToken cancellationToken = default)
    {
        var entry = await _entryRepo.GetByIdAsync(entryId, cancellationToken)
            ?? throw new KeyNotFoundException("Configuration entry not found.");

        var target = await _versionRepo.GetByEntryAndVersionAsync(entryId, targetVersion, cancellationToken)
            ?? throw new KeyNotFoundException($"Version {targetVersion} not found.");

        var previousValue = entry.ValueJson;
        var newVersion = entry.CurrentVersion + 1;

        var version = new ConfigVersion
        {
            Id = Guid.NewGuid(), ConfigurationEntryId = entryId,
            VersionNumber = newVersion, ValueJson = target.ValueJson,
            ChangeDescription = $"Rollback to version {targetVersion}.",
            ChangedByUserId = userId, ChangedByName = userName
        };

        entry.ValueJson = target.ValueJson;
        entry.CurrentVersion = newVersion;
        entry.UpdatedAt = DateTime.UtcNow;
        entry.UpdatedByUserId = userId;

        await _versionRepo.AddAsync(version, cancellationToken);
        await _entryRepo.UpdateAsync(entry, cancellationToken);

        await LogAuditAsync(entryId, entry.Key, "Rollback", previousValue, target.ValueJson, userId, userName, entry.CurrentVersion - 1, newVersion, cancellationToken);

        _cache.Remove($"config:{entry.Key}");
        _logger.LogInformation("Config rolled back: {Key} → v{Version} (from v{Old})", entry.Key, targetVersion, entry.CurrentVersion - 1);

        return entry;
    }

    public async Task<ConfigDiffDto> GetDiffAsync(Guid entryId, int versionA, int versionB, CancellationToken cancellationToken = default)
    {
        var entry = await _entryRepo.GetByIdAsync(entryId, cancellationToken)
            ?? throw new KeyNotFoundException("Configuration entry not found.");

        var va = await _versionRepo.GetByEntryAndVersionAsync(entryId, versionA, cancellationToken);
        var vb = await _versionRepo.GetByEntryAndVersionAsync(entryId, versionB, cancellationToken);

        return new ConfigDiffDto
        {
            Key = entry.Key, VersionA = versionA, VersionB = versionB,
            ValueA = va != null ? JsonSerializer.Deserialize<object>(va.ValueJson) : null,
            ValueB = vb != null ? JsonSerializer.Deserialize<object>(vb.ValueJson) : null,
            Changes = new List<string> { $"Diff between v{versionA} and v{versionB}" }
        };
    }

    public async Task<IReadOnlyList<ConfigAuditLogDto>> GetAuditLogAsync(Guid? entryId = null, Guid? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ConfigAuditLog> logs;

        if (entryId.HasValue)
            logs = await _auditRepo.GetByEntryAsync(entryId.Value, cancellationToken);
        else if (userId.HasValue)
            logs = await _auditRepo.GetByUserAsync(userId.Value, cancellationToken);
        else if (from.HasValue && to.HasValue)
            logs = await _auditRepo.GetByDateRangeAsync(from.Value, to.Value, cancellationToken);
        else
            logs = await _auditRepo.FindAsync(_ => true, cancellationToken);

        return logs.Select(l => new ConfigAuditLogDto
        {
            Id = l.Id, ConfigKey = l.ConfigKey, Action = l.Action,
            PreviousValue = l.PreviousValueJson != null ? JsonSerializer.Deserialize<object>(l.PreviousValueJson) : null,
            NewValue = l.NewValueJson != null ? JsonSerializer.Deserialize<object>(l.NewValueJson) : null,
            FromVersion = l.FromVersion, ToVersion = l.ToVersion,
            ChangedByName = l.ChangedByName, ChangedAt = l.ChangedAt
        }).ToList();
    }

    private async Task LogAuditAsync(Guid entryId, string key, string action, string? prev, string? next, Guid userId, string? userName, int? fromVersion, int? toVersion, CancellationToken ct)
    {
        var log = new ConfigAuditLog
        {
            Id = Guid.NewGuid(), ConfigurationEntryId = entryId, ConfigKey = key,
            Action = action, PreviousValueJson = prev, NewValueJson = next,
            FromVersion = fromVersion, ToVersion = toVersion,
            ChangedByUserId = userId, ChangedByName = userName
        };

        await _auditRepo.AddAsync(log, ct);
    }

    private static ConfigDto MapToDto(ConfigurationEntry e)
    {
        return new ConfigDto
        {
            Id = e.Id, Key = e.Key, Domain = e.Domain.ToString(),
            Value = JsonSerializer.Deserialize<object>(e.ValueJson),
            CurrentVersion = e.CurrentVersion, Status = e.Status.ToString(),
            Environment = e.Environment.ToString(), Description = e.Description, UpdatedAt = e.UpdatedAt
        };
    }
}
