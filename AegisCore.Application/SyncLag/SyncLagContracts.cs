using AegisCore.Domain.Entities;

namespace AegisCore.Application.SyncLag;

public interface ISyncLagService
{
    Task<SyncLagEntry> RecordWriteAsync(Guid replicaId, string entityType, Guid entityId, string? fieldName, string? originalValue, string writtenValue, Guid writtenByUserId, CancellationToken ct);
    Task<bool> CorrectEntryAsync(Guid entryId, string correctedValue, string reason, Guid correctedByUserId, CancellationToken ct);
    Task<int> CommitExpiredEntriesAsync(CancellationToken ct);
    Task<List<SyncLagEntry>> GetPendingEntriesAsync(Guid? replicaId, CancellationToken ct);
    Task<List<SyncLagEntry>> GetCommittedEntriesAsync(Guid? replicaId, int limit, CancellationToken ct);
    Task<SyncLagConfig> GetConfigAsync(CancellationToken ct);
    Task<SyncLagConfig> UpdateConfigAsync(SyncLagConfig config, CancellationToken ct);
    Task<bool> RevertEntryAsync(Guid entryId, Guid revertedByUserId, CancellationToken ct);
}

public class SyncLagDashboardDto
{
    public int TotalPending { get; set; }
    public int ExpiringWithin5Min { get; set; }
    public int CommittedToday { get; set; }
    public int CorrectedToday { get; set; }
    public int DefaultGraceSeconds { get; set; }
    public bool AutoCommitEnabled { get; set; }
    public List<SyncLagEntryDto> RecentEntries { get; set; } = new();
}

public class SyncLagEntryDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = "";
    public string? FieldName { get; set; }
    public string? OriginalValue { get; set; }
    public string? WrittenValue { get; set; }
    public string Status { get; set; } = "";
    public DateTime WrittenAt { get; set; }
    public DateTime GraceExpiresAt { get; set; }
    public int SecondsRemaining { get; set; }
    public bool IsLocked { get; set; }
    public bool CanCorrect { get; set; }
}
