using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface INotificationTemplateRepository : IRepository<NotificationTemplate>
{
    Task<IReadOnlyList<NotificationTemplate>> GetByChannelAsync(NotificationTemplateChannel channel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationTemplate>> GetByModuleAsync(string module, CancellationToken cancellationToken = default);
    Task<NotificationTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

public interface INotificationDeliveryRepository : IRepository<NotificationDelivery>
{
    Task<IReadOnlyList<NotificationDelivery>> GetByStatusAsync(DeliveryStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDelivery>> GetPendingRetryAsync(CancellationToken cancellationToken = default);
}

public interface IFeatureFlagRepository : IRepository<FeatureFlag>
{
    Task<FeatureFlag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeatureFlag>> GetByModuleAsync(string module, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeatureFlag>> GetEnabledAsync(CancellationToken cancellationToken = default);
}

public interface IScheduledJobRepository : IRepository<ScheduledJob>
{
    Task<IReadOnlyList<ScheduledJob>> GetEnabledAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduledJob>> GetByCategoryAsync(JobCategory category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduledJob>> GetDueJobsAsync(DateTime before, CancellationToken cancellationToken = default);
}

public interface IJobExecutionRepository : IRepository<JobExecution>
{
    Task<IReadOnlyList<JobExecution>> GetByJobAsync(Guid jobId, int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobExecution>> GetByStatusAsync(JobExecutionStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobExecution>> GetRecentFailuresAsync(int limit = 20, CancellationToken cancellationToken = default);
}

public interface IImportBatchRepository : IRepository<ImportBatch>
{
    Task<IReadOnlyList<ImportBatch>> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportBatch>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}

public interface IExportLogRepository : IRepository<ExportLog>
{
    Task<IReadOnlyList<ExportLog>> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExportLog>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IPlatformMetricRepository : IRepository<PlatformMetric>
{
    Task<IReadOnlyList<PlatformMetric>> GetByCategoryAsync(string category, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformMetric>> GetRecentAsync(string metricName, int limit = 50, CancellationToken cancellationToken = default);
}
