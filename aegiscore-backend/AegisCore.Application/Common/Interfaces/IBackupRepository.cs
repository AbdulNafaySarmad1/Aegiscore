using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IBackupConfigurationRepository : IRepository<BackupConfiguration>
{
    Task<IReadOnlyList<BackupConfiguration>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupConfiguration>> GetScheduledBeforeAsync(DateTime before, CancellationToken cancellationToken = default);
}

public interface IBackupRecordRepository : IRepository<BackupRecord>
{
    Task<IReadOnlyList<BackupRecord>> GetByConfigurationAsync(Guid configId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupRecord>> GetByStatusAsync(BackupStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupRecord>> GetExpiredAsync(DateTime now, CancellationToken cancellationToken = default);
    Task<BackupRecord?> GetLatestSuccessfulAsync(Guid configId, CancellationToken cancellationToken = default);
}

public interface IReadReplicaConfigRepository : IRepository<ReadReplicaConfig>
{
    Task<IReadOnlyList<ReadReplicaConfig>> GetOnlineAsync(CancellationToken cancellationToken = default);
    Task<ReadReplicaConfig?> GetByPriorityAsync(int priority, CancellationToken cancellationToken = default);
}
