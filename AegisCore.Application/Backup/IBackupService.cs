namespace AegisCore.Application.Backup;

public interface IBackupService
{
    Task<BackupRecord> RunBackupAsync(Guid configurationId, string triggeredBy, CancellationToken cancellationToken = default);
    Task<BackupRecord> RunAllScheduledBackupsAsync(CancellationToken cancellationToken = default);
    Task VerifyBackupAsync(Guid recordId, CancellationToken cancellationToken = default);
    Task CleanupExpiredBackupsAsync(CancellationToken cancellationToken = default);
    Task<BackupRecord> RestoreFromBackupAsync(Guid recordId, DateTime? pointInTime = null, CancellationToken cancellationToken = default);
}

public interface IDatabaseHealthService
{
    Task<bool> IsPrimaryHealthyAsync(CancellationToken cancellationToken = default);
    Task<bool> IsReplicaLaggingAsync(Guid replicaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Entities.ReadReplicaConfig>> GetHealthyReplicasAsync(CancellationToken cancellationToken = default);
    Task PromoteReplicaAsync(Guid replicaId, CancellationToken cancellationToken = default);
}
