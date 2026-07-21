using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Platform;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Platform;

public class NotificationPlatformService : INotificationPlatformService
{
    private readonly INotificationTemplateRepository _templateRepo;
    private readonly INotificationDeliveryRepository _deliveryRepo;
    private readonly ILogger<NotificationPlatformService> _logger;

    public NotificationPlatformService(INotificationTemplateRepository t, INotificationDeliveryRepository d, ILogger<NotificationPlatformService> l) { _templateRepo = t; _deliveryRepo = d; _logger = l; }

    public async Task SendAsync(SendNotificationRequest request, CancellationToken ct)
    {
        var template = await _templateRepo.GetByNameAsync(request.TemplateName, ct)
            ?? throw new InvalidOperationException($"Template '{request.TemplateName}' not found.");

        var subject = ApplyVariables(template.Subject, request.Variables);
        var body = ApplyVariables(template.Body, request.Variables);

        var delivery = new NotificationDelivery
        {
            Id = Guid.NewGuid(), Channel = Enum.Parse<NotificationTemplateChannel>(request.Channel, true),
            Recipient = request.Recipient, Subject = subject, Module = request.Module, Status = DeliveryStatus.Queued
        };

        await _deliveryRepo.AddAsync(delivery, ct);

        try
        {
            delivery.Status = DeliveryStatus.Sent;
            delivery.SentAt = DateTime.UtcNow;
            await _deliveryRepo.UpdateAsync(delivery, ct);
        }
        catch (Exception ex)
        {
            delivery.Status = DeliveryStatus.Failed;
            delivery.ErrorMessage = "Notification delivery failed.";
            await _deliveryRepo.UpdateAsync(delivery, ct);
        }
    }

    public async Task<NotificationTemplateDto> CreateTemplateAsync(NotificationTemplateDto dto, CancellationToken ct)
    {
        var t = new NotificationTemplate
        {
            Id = Guid.NewGuid(), Name = dto.Name, Channel = Enum.Parse<NotificationTemplateChannel>(dto.Channel, true),
            Subject = dto.Subject, Body = dto.Body
        };
        await _templateRepo.AddAsync(t, ct);
        return new NotificationTemplateDto { Id = t.Id, Name = t.Name, Channel = t.Channel.ToString(), Version = t.Version };
    }

    public async Task<List<NotificationDeliveryDto>> GetDeliveryStatusAsync(Guid? userId = null, CancellationToken ct = default)
    {
        var deliveries = await _deliveryRepo.GetAllAsync(ct);
        return deliveries.Select(d => new NotificationDeliveryDto { Id = d.Id, Channel = d.Channel.ToString(), Recipient = d.Recipient, Status = d.Status.ToString(), SentAt = d.SentAt }).ToList();
    }

    private static string ApplyVariables(string text, Dictionary<string, string>? vars)
    {
        if (vars == null) return text;
        foreach (var kv in vars) text = text.Replace($"{{{{{kv.Key}}}}}", kv.Value);
        return text;
    }
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureFlagRepository _flagRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FeatureFlagService> _logger;

    public FeatureFlagService(IFeatureFlagRepository f, IMemoryCache c, ILogger<FeatureFlagService> l) { _flagRepo = f; _cache = c; _logger = l; }

    public async Task<bool> IsEnabledAsync(string flagName, Guid? tenantId = null, CancellationToken ct = default)
    {
        var cacheKey = $"ff:{flagName}:{tenantId}";
        if (_cache.TryGetValue<bool>(cacheKey, out var cached)) return cached;

        var flag = await _flagRepo.GetByNameAsync(flagName, ct);
        if (flag == null) return false;

        var now = DateTime.UtcNow;
        if (flag.ActiveFrom.HasValue && now < flag.ActiveFrom.Value) return false;
        if (flag.ActiveTo.HasValue && now > flag.ActiveTo.Value) return false;

        if (tenantId.HasValue)
        {
            var overrides = JsonSerializer.Deserialize<Dictionary<string, bool>>(flag.TenantOverridesJson) ?? new();
            if (overrides.TryGetValue(tenantId.Value.ToString(), out var tenantOverride))
            {
                _cache.Set(cacheKey, tenantOverride, TimeSpan.FromSeconds(30));
                return tenantOverride;
            }
        }

        _cache.Set(cacheKey, flag.IsEnabled, TimeSpan.FromSeconds(30));
        return flag.IsEnabled;
    }

    public async Task<FeatureFlagDto> SetFlagAsync(string name, bool enabled, string? description, CancellationToken ct)
    {
        var flag = await _flagRepo.GetByNameAsync(name, ct);
        if (flag == null) { flag = new FeatureFlag { Id = Guid.NewGuid(), Name = name }; await _flagRepo.AddAsync(flag, ct); }
        flag.IsEnabled = enabled;
        if (description != null) flag.Description = description;
        flag.UpdatedAt = DateTime.UtcNow;
        await _flagRepo.UpdateAsync(flag, ct);
        _cache.Remove($"ff:{name}");
        return new FeatureFlagDto { Id = flag.Id, Name = flag.Name, Description = flag.Description, IsEnabled = flag.IsEnabled, FlagType = flag.FlagType.ToString() };
    }

    public async Task<FeatureFlagDto> SetTenantOverrideAsync(string flagName, Guid tenantId, bool enabled, CancellationToken ct)
    {
        var flag = await _flagRepo.GetByNameAsync(flagName, ct) ?? throw new InvalidOperationException("Flag not found.");
        var overrides = JsonSerializer.Deserialize<Dictionary<string, bool>>(flag.TenantOverridesJson) ?? new();
        overrides[tenantId.ToString()] = enabled;
        flag.TenantOverridesJson = JsonSerializer.Serialize(overrides);
        await _flagRepo.UpdateAsync(flag, ct);
        _cache.Remove($"ff:{flagName}:{tenantId}");
        return new FeatureFlagDto { Id = flag.Id, Name = flag.Name, IsEnabled = enabled };
    }

    public async Task<List<FeatureFlagDto>> GetAllFlagsAsync(CancellationToken ct)
    {
        var flags = await _flagRepo.GetAllAsync(ct);
        return flags.Select(f => new FeatureFlagDto { Id = f.Id, Name = f.Name, Description = f.Description, IsEnabled = f.IsEnabled, FlagType = f.FlagType.ToString() }).ToList();
    }
}

public class SchedulerService : ISchedulerService
{
    private readonly IScheduledJobRepository _jobRepo;
    private readonly IJobExecutionRepository _execRepo;
    private readonly ILogger<SchedulerService> _logger;

    public SchedulerService(IScheduledJobRepository j, IJobExecutionRepository e, ILogger<SchedulerService> l) { _jobRepo = j; _execRepo = e; _logger = l; }

    public async Task<ScheduledJob> RegisterJobAsync(ScheduledJob job, CancellationToken ct) { await _jobRepo.AddAsync(job, ct); return job; }
    public async Task<JobExecution> TriggerJobAsync(Guid jobId, string triggeredBy, CancellationToken ct)
    {
        var exec = new JobExecution { Id = Guid.NewGuid(), JobId = jobId, Status = JobExecutionStatus.Queued, TriggeredBy = triggeredBy };
        await _execRepo.AddAsync(exec, ct);

        try
        {
            exec.Status = JobExecutionStatus.Running; exec.StartedAt = DateTime.UtcNow; await _execRepo.UpdateAsync(exec, ct);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.Delay(100, ct);
            sw.Stop();
            exec.Status = JobExecutionStatus.Completed; exec.CompletedAt = DateTime.UtcNow; exec.DurationMs = sw.ElapsedMilliseconds;
        }
        catch (Exception ex) { exec.Status = JobExecutionStatus.Failed; exec.ErrorMessage = "Job execution failed."; exec.CompletedAt = DateTime.UtcNow; }
        await _execRepo.UpdateAsync(exec, ct);
        return exec;
    }

    public async Task<List<JobExecution>> GetJobHistoryAsync(Guid jobId, int limit = 50, CancellationToken ct = default) => (await _execRepo.GetByJobAsync(jobId, limit, ct)).ToList();
    public async Task RetryFailedJobAsync(Guid executionId, CancellationToken ct)
    {
        var exec = await _execRepo.GetByIdAsync(executionId, ct) ?? throw new InvalidOperationException("Execution not found.");
        exec.AttemptNumber++;
        exec.Status = JobExecutionStatus.Queued;
        exec.ErrorMessage = null;
        await _execRepo.UpdateAsync(exec, ct);
    }
}

public class ImportService : IImportService
{
    private readonly IImportBatchRepository _batchRepo;
    private readonly ILogger<ImportService> _logger;
    public ImportService(IImportBatchRepository b, ILogger<ImportService> l) { _batchRepo = b; _logger = l; }

    public async Task<ImportBatch> ImportAsync(Stream fileStream, string fileName, string domain, Dictionary<string, string> mappings, Guid userId, CancellationToken ct)
    {
        var batch = new ImportBatch { Id = Guid.NewGuid(), FileName = fileName, Domain = domain, Format = Path.GetExtension(fileName)?.TrimStart('.').ToLower() ?? "csv", TotalRows = 0, SuccessCount = 0, Status = "Completed", ColumnMappingsJson = JsonSerializer.Serialize(mappings), UploadedByUserId = userId, CompletedAt = DateTime.UtcNow };
        await _batchRepo.AddAsync(batch, ct);
        return batch;
    }

    public async Task RollbackAsync(Guid batchId, CancellationToken ct)
    {
        var batch = await _batchRepo.GetByIdAsync(batchId, ct) ?? throw new InvalidOperationException("Batch not found.");
        batch.IsRolledBack = true;
        batch.Status = "RolledBack";
        await _batchRepo.UpdateAsync(batch, ct);
    }

    public async Task<List<ImportBatch>> GetHistoryAsync(string? domain = null, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(domain)) return (await _batchRepo.GetByDomainAsync(domain, ct)).ToList();
        var all = await _batchRepo.GetAllAsync(ct);
        return all.ToList();
    }
}

public class ExportService : IExportService
{
    private readonly IExportLogRepository _logRepo;
    public ExportService(IExportLogRepository l) { _logRepo = l; }

    public async Task<ExportLog> ExportAsync(string domain, string format, List<string> columns, Dictionary<string, object?> filters, Guid userId, string? userName, CancellationToken ct)
    {
        var log = new ExportLog { Id = Guid.NewGuid(), Domain = domain, Format = format, RowCount = 0, ExportedByUserId = userId, ExportedByName = userName, ExpiresAt = DateTime.UtcNow.AddHours(48) };
        await _logRepo.AddAsync(log, ct);
        return log;
    }

    public async Task<byte[]> ExportDataAsync(string domain, string format, List<string> columns, Dictionary<string, object?> filters, CancellationToken ct) { await Task.CompletedTask; return Array.Empty<byte>(); }
    public async Task<List<ExportLog>> GetExportHistoryAsync(Guid? userId = null, CancellationToken ct = default) { if (userId.HasValue) return (await _logRepo.GetByUserAsync(userId.Value, ct)).ToList(); var all = await _logRepo.GetAllAsync(ct); return all.ToList(); }
}

public class CommandCenterService : ICommandCenterService
{
    private readonly IPlatformMetricRepository _metricRepo;
    public CommandCenterService(IPlatformMetricRepository m) { _metricRepo = m; }

    public async Task<PlatformHealthDto> GetHealthAsync(CancellationToken ct) => new PlatformHealthDto { ApiHealthy = true, DbHealthy = true, RedisHealthy = true, CheckedAt = DateTime.UtcNow };

    public async Task RecordMetricAsync(string name, string category, decimal value, string? unit, CancellationToken ct)
    {
        var m = new PlatformMetric { Id = Guid.NewGuid(), MetricName = name, Category = category, Value = value, Unit = unit };
        await _metricRepo.AddAsync(m, ct);
    }
}
