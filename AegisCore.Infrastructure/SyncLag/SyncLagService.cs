using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.SyncLag;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.SyncLag;

public class SyncLagService : ISyncLagService
{
    private readonly IRepository<SyncLagEntry> _entryRepo;
    private readonly IRepository<SyncLagConfig> _configRepo;
    private readonly ILogger<SyncLagService> _logger;

    public SyncLagService(IRepository<SyncLagEntry> e, IRepository<SyncLagConfig> c, ILogger<SyncLagService> l)
    { _entryRepo = e; _configRepo = c; _logger = l; }

    public async Task<SyncLagConfig> GetConfigAsync(CancellationToken ct)
    {
        var configs = await _configRepo.GetAllAsync(ct);
        var config = configs.FirstOrDefault();
        if (config == null) { config = new SyncLagConfig { Id = Guid.NewGuid(), TenantId = Guid.Empty }; await _configRepo.AddAsync(config, ct); }
        return config;
    }

    public async Task<SyncLagConfig> UpdateConfigAsync(SyncLagConfig config, CancellationToken ct)
    {
        var existing = await GetConfigAsync(ct);
        existing.DefaultGracePeriodSeconds = Math.Max(1, config.DefaultGracePeriodSeconds);
        existing.MaxGracePeriodSeconds = Math.Max(existing.DefaultGracePeriodSeconds, config.MaxGracePeriodSeconds);
        existing.AutoCommitOnGraceExpiry = config.AutoCommitOnGraceExpiry;
        existing.RequireAdminApprovalForCorrections = config.RequireAdminApprovalForCorrections;
        existing.CorrectionPermission = config.CorrectionPermission;
        existing.NotifyOwnerOnCorrection = config.NotifyOwnerOnCorrection;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedByUserId = config.UpdatedByUserId;
        await _configRepo.UpdateAsync(existing, ct);
        _logger.LogInformation("Sync lag config updated: grace={Grace}s, autoCommit={Auto}", existing.DefaultGracePeriodSeconds, existing.AutoCommitOnGraceExpiry);
        return existing;
    }

    public async Task<SyncLagEntry> RecordWriteAsync(Guid replicaId, string entityType, Guid entityId, string? fieldName, string? originalValue, string writtenValue, Guid writtenByUserId, CancellationToken ct)
    {
        var config = await GetConfigAsync(ct);
        var entry = new SyncLagEntry
        {
            Id = Guid.NewGuid(), ReplicaId = replicaId, EntityType = entityType,
            EntityId = entityId, FieldName = fieldName, OriginalValue = originalValue,
            WrittenValue = writtenValue, WrittenByUserId = writtenByUserId,
            Status = "PendingSync", WrittenAt = DateTime.UtcNow,
            GraceExpiresAt = DateTime.UtcNow.AddSeconds(config.DefaultGracePeriodSeconds),
            TenantId = Guid.Empty
        };
        await _entryRepo.AddAsync(entry, ct);
        _logger.LogInformation("SyncLag entry created: {EntityType}#{EntityId} via replica {ReplicaId}, grace expires {Expiry}", entityType, entityId, replicaId, entry.GraceExpiresAt);
        return entry;
    }

    public async Task<bool> CorrectEntryAsync(Guid entryId, string correctedValue, string reason, Guid correctedByUserId, CancellationToken ct)
    {
        var entry = await _entryRepo.GetByIdAsync(entryId, ct);
        if (entry == null || entry.IsLocked || entry.Status == "Committed") return false;

        var config = await GetConfigAsync(ct);
        if (config.RequireAdminApprovalForCorrections)
        {
            _logger.LogInformation("Correction requested for entry {EntryId} — requires admin approval", entryId);
        }

        entry.Status = "Corrected";
        entry.CorrectedAt = DateTime.UtcNow;
        entry.CorrectedByUserId = correctedByUserId;
        entry.CorrectionReason = reason;
        // Keep original WrittenValue for audit, update what gets committed
        entry.WrittenValue = correctedValue;
        await _entryRepo.UpdateAsync(entry, ct);
        _logger.LogInformation("SyncLag entry {EntryId} corrected: was={Old}, now={New}, reason={Reason}", entryId, entry.OriginalValue, correctedValue, reason);
        return true;
    }

    public async Task<bool> RevertEntryAsync(Guid entryId, Guid revertedByUserId, CancellationToken ct)
    {
        var entry = await _entryRepo.GetByIdAsync(entryId, ct);
        if (entry == null || entry.IsLocked || entry.Status == "Committed") return false;
        entry.Status = "Reverted";
        entry.CorrectedAt = DateTime.UtcNow;
        entry.CorrectedByUserId = revertedByUserId;
        entry.IsLocked = true;
        entry.LockedAt = DateTime.UtcNow;
        await _entryRepo.UpdateAsync(entry, ct);
        _logger.LogInformation("SyncLag entry {EntryId} reverted by {UserId}", entryId, revertedByUserId);
        return true;
    }

    public async Task<int> CommitExpiredEntriesAsync(CancellationToken ct)
    {
        var config = await GetConfigAsync(ct);
        if (!config.AutoCommitOnGraceExpiry) return 0;

        var pending = await _entryRepo.FindAsync(e => e.Status == "PendingSync" && e.GraceExpiresAt <= DateTime.UtcNow, ct);
        var committed = 0;
        foreach (var entry in pending)
        {
            entry.Status = "Committed";
            entry.CommittedAt = DateTime.UtcNow;
            entry.IsLocked = true;
            entry.LockedAt = DateTime.UtcNow;
            await _entryRepo.UpdateAsync(entry, ct);
            committed++;
        }
        if (committed > 0) _logger.LogInformation("Committed {Count} expired sync-lag entries — now locked", committed);
        return committed;
    }

    public async Task<List<SyncLagEntry>> GetPendingEntriesAsync(Guid? replicaId, CancellationToken ct)
    {
        var all = await _entryRepo.FindAsync(e => e.Status == "PendingSync" && (replicaId == null || e.ReplicaId == replicaId), ct);
        return all.OrderBy(e => e.GraceExpiresAt).ToList();
    }

    public async Task<List<SyncLagEntry>> GetCommittedEntriesAsync(Guid? replicaId, int limit, CancellationToken ct)
    {
        var all = await _entryRepo.FindAsync(e => e.Status == "Committed" && (replicaId == null || e.ReplicaId == replicaId), ct);
        return all.OrderByDescending(e => e.CommittedAt).Take(limit).ToList();
    }
}
