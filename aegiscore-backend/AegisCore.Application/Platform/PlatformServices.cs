namespace AegisCore.Application.Platform;

public interface INotificationPlatformService
{
    Task SendAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);
    Task<NotificationTemplateDto> CreateTemplateAsync(NotificationTemplateDto template, CancellationToken cancellationToken = default);
    Task<List<NotificationDeliveryDto>> GetDeliveryStatusAsync(Guid? userId = null, CancellationToken cancellationToken = default);
}

public interface INotificationRealtimePublisher
{
    Task PublishAsync(string recipient, NotificationDeliveryDto delivery, CancellationToken cancellationToken = default);
}

public interface IGrievanceService
{
    Task<GrievanceTicketDto> CreateAsync(CreateGrievanceTicketRequest request, Guid submitterId, CancellationToken cancellationToken = default);
    Task<GrievanceTicketDto> RespondAsync(Guid ticketId, GrievanceResponseRequest request, Guid responderId, CancellationToken cancellationToken = default);
    Task<List<GrievanceTicketDto>> ListAsync(Guid? submitterId = null, CancellationToken cancellationToken = default);
}

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string flagName, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<FeatureFlagDto> SetFlagAsync(string name, bool enabled, string? description, CancellationToken cancellationToken = default);
    Task<FeatureFlagDto> SetTenantOverrideAsync(string flagName, Guid tenantId, bool enabled, CancellationToken cancellationToken = default);
    Task<List<FeatureFlagDto>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
}

public interface ISchedulerService
{
    Task<ScheduledJob> RegisterJobAsync(ScheduledJob job, CancellationToken cancellationToken = default);
    Task<JobExecution> TriggerJobAsync(Guid jobId, string triggeredBy, CancellationToken cancellationToken = default);
    Task<List<JobExecution>> GetJobHistoryAsync(Guid jobId, int limit = 50, CancellationToken cancellationToken = default);
    Task RetryFailedJobAsync(Guid executionId, CancellationToken cancellationToken = default);
}

public interface IImportService
{
    Task<ImportBatch> ImportAsync(Stream fileStream, string fileName, string domain, Dictionary<string, string> columnMappings, Guid userId, CancellationToken cancellationToken = default);
    Task RollbackAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<List<ImportBatch>> GetHistoryAsync(string? domain = null, CancellationToken cancellationToken = default);
}

public interface IExportService
{
    Task<ExportLog> ExportAsync(string domain, string format, List<string> columns, Dictionary<string, object?> filters, Guid userId, string? userName, CancellationToken cancellationToken = default);
    Task<byte[]> ExportDataAsync(string domain, string format, List<string> columns, Dictionary<string, object?> filters, CancellationToken cancellationToken = default);
    Task<List<ExportLog>> GetExportHistoryAsync(Guid? userId = null, CancellationToken cancellationToken = default);
}

public interface ICommandCenterService
{
    Task<PlatformHealthDto> GetHealthAsync(CancellationToken cancellationToken = default);
    Task RecordMetricAsync(string name, string category, decimal value, string? unit, CancellationToken cancellationToken = default);
}

// DTOs
public class SendNotificationRequest { public string TemplateName { get; set; } = ""; public string Channel { get; set; } = "Email"; public string Recipient { get; set; } = ""; public string? Module { get; set; } public Dictionary<string, string>? Variables { get; set; } }
public class NotificationTemplateDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string Channel { get; set; } = ""; public string Subject { get; set; } = ""; public string Body { get; set; } = ""; public int Version { get; set; } }
public class NotificationDeliveryDto { public Guid Id { get; set; } public string Channel { get; set; } = ""; public string Recipient { get; set; } = ""; public string Status { get; set; } = ""; public DateTime? SentAt { get; set; } }
public class CreateGrievanceTicketRequest { public string Category { get; set; } = "other"; public string Subject { get; set; } = ""; public string Description { get; set; } = ""; public string Priority { get; set; } = "normal"; }
public class GrievanceResponseRequest { public string ResponseText { get; set; } = ""; public string? StatusUpdate { get; set; } }
public class GrievanceTicketDto { public Guid Id { get; set; } public string TicketNumber { get; set; } = ""; public string Category { get; set; } = ""; public string Subject { get; set; } = ""; public string Status { get; set; } = ""; public string Priority { get; set; } = ""; public DateTime SubmittedAt { get; set; } public DateTime? ResolvedAt { get; set; } }
public class FeatureFlagDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string? Description { get; set; } public bool IsEnabled { get; set; } public string FlagType { get; set; } = ""; }
public class JobDto { public Guid Id { get; set; } public string Name { get; set; } = ""; public string CronExpression { get; set; } = ""; public bool IsEnabled { get; set; } public string Status { get; set; } = ""; public DateTime? LastRunAt { get; set; } public DateTime? NextRunAt { get; set; } }
public class JobExecutionDto { public Guid Id { get; set; } public string Status { get; set; } = ""; public DateTime? StartedAt { get; set; } public long? DurationMs { get; set; } public string? ErrorMessage { get; set; } }
public class ImportBatchDto { public Guid Id { get; set; } public string FileName { get; set; } = ""; public string Domain { get; set; } = ""; public int TotalRows { get; set; } public int SuccessCount { get; set; } public int ErrorCount { get; set; } public string Status { get; set; } = ""; }
public class ExportLogDto { public Guid Id { get; set; } public string Domain { get; set; } = ""; public string Format { get; set; } = ""; public int RowCount { get; set; } public DateTime CreatedAt { get; set; } }
public class PlatformHealthDto { public bool ApiHealthy { get; set; } public bool DbHealthy { get; set; } public bool RedisHealthy { get; set; } public int ActiveJobs { get; set; } public int FailedJobs24h { get; set; } public int PendingNotifications { get; set; } public int QueueDepth { get; set; } public DateTime CheckedAt { get; set; } }
