using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class NotificationTemplateRepository : Repository<NotificationTemplate>, INotificationTemplateRepository
{
    public NotificationTemplateRepository(AegisDbContext c) : base(c) { }
    public async Task<IReadOnlyList<NotificationTemplate>> GetByChannelAsync(NotificationTemplateChannel ch, CancellationToken ct) => await _dbSet.Where(t => t.Channel == ch).ToListAsync(ct);
    public async Task<IReadOnlyList<NotificationTemplate>> GetByModuleAsync(string m, CancellationToken ct) => await _dbSet.Where(t => t.Module == m).ToListAsync(ct);
    public async Task<NotificationTemplate?> GetByNameAsync(string n, CancellationToken ct) => await _dbSet.FirstOrDefaultAsync(t => t.Name == n, ct);
}

public class NotificationDeliveryRepository : Repository<NotificationDelivery>, INotificationDeliveryRepository
{
    public NotificationDeliveryRepository(AegisDbContext c) : base(c) { }
    public async Task<IReadOnlyList<NotificationDelivery>> GetByStatusAsync(DeliveryStatus s, CancellationToken ct) => await _dbSet.Where(d => d.Status == s).ToListAsync(ct);
    public async Task<IReadOnlyList<NotificationDelivery>> GetPendingRetryAsync(CancellationToken ct) => await _dbSet.Where(d => d.Status == DeliveryStatus.Failed && d.RetryCount < 3).ToListAsync(ct);
}

public class FeatureFlagRepository : Repository<FeatureFlag>, IFeatureFlagRepository
{
    public FeatureFlagRepository(AegisDbContext c) : base(c) { }
    public async Task<FeatureFlag?> GetByNameAsync(string n, CancellationToken ct) => await _dbSet.FirstOrDefaultAsync(f => f.Name == n, ct);
    public async Task<IReadOnlyList<FeatureFlag>> GetByModuleAsync(string m, CancellationToken ct) => await _dbSet.Where(f => f.Module == m).ToListAsync(ct);
    public async Task<IReadOnlyList<FeatureFlag>> GetEnabledAsync(CancellationToken ct) => await _dbSet.Where(f => f.IsEnabled).ToListAsync(ct);
}

public class ScheduledJobRepository : Repository<ScheduledJob>, IScheduledJobRepository
{
    public ScheduledJobRepository(AegisDbContext c) : base(c) { }
    public async Task<IReadOnlyList<ScheduledJob>> GetEnabledAsync(CancellationToken ct) => await _dbSet.Where(j => j.IsEnabled).ToListAsync(ct);
    public async Task<IReadOnlyList<ScheduledJob>> GetByCategoryAsync(JobCategory cat, CancellationToken ct) => await _dbSet.Where(j => j.Category == cat).ToListAsync(ct);
    public async Task<IReadOnlyList<ScheduledJob>> GetDueJobsAsync(DateTime before, CancellationToken ct) => await _dbSet.Where(j => j.IsEnabled && j.NextRunAt.HasValue && j.NextRunAt.Value <= before).ToListAsync(ct);
}

public class JobExecutionRepository : Repository<JobExecution>, IJobExecutionRepository
{
    public JobExecutionRepository(AegisDbContext c) : base(c) { }
    public async Task<IReadOnlyList<JobExecution>> GetByJobAsync(Guid jId, int limit, CancellationToken ct) => await _dbSet.Where(e => e.JobId == jId).OrderByDescending(e => e.CreatedAt).Take(limit).ToListAsync(ct);
    public async Task<IReadOnlyList<JobExecution>> GetByStatusAsync(JobExecutionStatus s, CancellationToken ct) => await _dbSet.Where(e => e.Status == s).ToListAsync(ct);
    public async Task<IReadOnlyList<JobExecution>> GetRecentFailuresAsync(int limit, CancellationToken ct) => await _dbSet.Where(e => e.Status == JobExecutionStatus.Failed).OrderByDescending(e => e.CreatedAt).Take(limit).ToListAsync(ct);
}

public class ImportBatchRepository : Repository<ImportBatch>, IImportBatchRepository
{
    public ImportBatchRepository(AegisDbContext c) : base(c) { }
    public async Task<IReadOnlyList<ImportBatch>> GetByDomainAsync(string d, CancellationToken ct) => await _dbSet.Where(b => b.Domain == d).OrderByDescending(b => b.CreatedAt).ToListAsync(ct);
    public async Task<IReadOnlyList<ImportBatch>> GetByStatusAsync(string s, CancellationToken ct) => await _dbSet.Where(b => b.Status == s).ToListAsync(ct);
}

public class ExportLogRepository : Repository<ExportLog>, IExportLogRepository
{
    public ExportLogRepository(AegisDbContext c) : base(c) { }
    public async Task<IReadOnlyList<ExportLog>> GetByDomainAsync(string d, CancellationToken ct) => await _dbSet.Where(l => l.Domain == d).OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
    public async Task<IReadOnlyList<ExportLog>> GetByUserAsync(Guid u, CancellationToken ct) => await _dbSet.Where(l => l.ExportedByUserId == u).OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
}

public class PlatformMetricRepository : Repository<PlatformMetric>, IPlatformMetricRepository
{
    public PlatformMetricRepository(AegisDbContext c) : base(c) { }
    public async Task<IReadOnlyList<PlatformMetric>> GetByCategoryAsync(string cat, int limit, CancellationToken ct) => await _dbSet.Where(m => m.Category == cat).OrderByDescending(m => m.RecordedAt).Take(limit).ToListAsync(ct);
    public async Task<IReadOnlyList<PlatformMetric>> GetRecentAsync(string n, int limit, CancellationToken ct) => await _dbSet.Where(m => m.MetricName == n).OrderByDescending(m => m.RecordedAt).Take(limit).ToListAsync(ct);
}
