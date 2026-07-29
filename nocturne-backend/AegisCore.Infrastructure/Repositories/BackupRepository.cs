using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class BackupConfigurationRepository : Repository<BackupConfiguration>, IBackupConfigurationRepository
{
    public BackupConfigurationRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<BackupConfiguration>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.IsEnabled).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BackupConfiguration>> GetScheduledBeforeAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.IsEnabled && c.NextRunAt.HasValue && c.NextRunAt.Value <= before).ToListAsync(cancellationToken);
    }
}

public class BackupRecordRepository : Repository<BackupRecord>, IBackupRecordRepository
{
    public BackupRecordRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<BackupRecord>> GetByConfigurationAsync(Guid configId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.ConfigurationId == configId).OrderByDescending(r => r.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BackupRecord>> GetByStatusAsync(BackupStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BackupRecord>> GetExpiredAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.ExpiresAt.HasValue && r.ExpiresAt.Value <= now).ToListAsync(cancellationToken);
    }

    public async Task<BackupRecord?> GetLatestSuccessfulAsync(Guid configId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ConfigurationId == configId && r.Status == BackupStatus.Completed)
            .OrderByDescending(r => r.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class ReadReplicaConfigRepository : Repository<ReadReplicaConfig>, IReadReplicaConfigRepository
{
    public ReadReplicaConfigRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ReadReplicaConfig>> GetOnlineAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.Status == ReplicaStatus.Online && r.IsEnabled).OrderBy(r => r.Priority).ToListAsync(cancellationToken);
    }

    public async Task<ReadReplicaConfig?> GetByPriorityAsync(int priority, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.Priority == priority, cancellationToken);
    }
}
