using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Platform;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Platform;

public class NotificationPlatformService : INotificationPlatformService
{
    private readonly INotificationTemplateRepository _templateRepo;
    private readonly INotificationDeliveryRepository _deliveryRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly INotificationRealtimePublisher _realtimePublisher;
    private readonly ILogger<NotificationPlatformService> _logger;

    public NotificationPlatformService(
        INotificationTemplateRepository t,
        INotificationDeliveryRepository d,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        INotificationRealtimePublisher realtimePublisher,
        ILogger<NotificationPlatformService> l)
    {
        _templateRepo = t;
        _deliveryRepo = d;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _realtimePublisher = realtimePublisher;
        _logger = l;
    }

    public async Task SendAsync(SendNotificationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateName) || string.IsNullOrWhiteSpace(request.Recipient)) throw new ArgumentException("Template and recipient are required.");
        var template = await _templateRepo.GetByNameAsync(request.TemplateName, ct)
            ?? throw new InvalidOperationException($"Template '{request.TemplateName}' not found.");

        var subject = ApplyVariables(template.Subject, request.Variables);
        var body = ApplyVariables(template.Body, request.Variables);
        if (!Enum.TryParse<NotificationTemplateChannel>(request.Channel, true, out var channel)) throw new ArgumentException("Notification channel is invalid.");

        var delivery = new NotificationDelivery
        {
            Id = Guid.NewGuid(), Channel = channel,
            Recipient = request.Recipient, Subject = subject, Module = request.Module, Status = DeliveryStatus.Queued
        };

        await _deliveryRepo.AddAsync(delivery, ct);

        try
        {
            await DeliverAsync(channel, request.Recipient, subject, body, template.Name, request.Variables, ct);
            delivery.Status = DeliveryStatus.Sent;
            delivery.SentAt = DateTime.UtcNow;
            await _deliveryRepo.UpdateAsync(delivery, ct);
            await _realtimePublisher.PublishAsync(request.Recipient, ToDto(delivery), ct);
        }
        catch (Exception ex)
        {
            delivery.Status = DeliveryStatus.Failed;
            delivery.ErrorMessage = ex.Message;
            await _deliveryRepo.UpdateAsync(delivery, ct);
            _logger.LogWarning(ex, "Notification delivery failed for {Channel} recipient {Recipient}", channel, request.Recipient);
        }
    }

    public async Task<NotificationTemplateDto> CreateTemplateAsync(NotificationTemplateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Body)) throw new ArgumentException("Template name and body are required.");
        if (!Enum.TryParse<NotificationTemplateChannel>(dto.Channel, true, out var channel)) throw new ArgumentException("Notification channel is invalid.");
        var t = new NotificationTemplate
        {
            Id = Guid.NewGuid(), Name = dto.Name.Trim(), Channel = channel,
            Subject = dto.Subject, Body = dto.Body
        };
        await _templateRepo.AddAsync(t, ct);
        return new NotificationTemplateDto { Id = t.Id, Name = t.Name, Channel = t.Channel.ToString(), Version = t.Version };
    }

    public async Task<List<NotificationDeliveryDto>> GetDeliveryStatusAsync(Guid? userId = null, CancellationToken ct = default)
    {
        var deliveries = await _deliveryRepo.GetAllAsync(ct);
        return deliveries.Select(ToDto).ToList();
    }

    public static string ApplyVariables(string text, Dictionary<string, string>? vars)
    {
        if (vars == null) return text;
        foreach (var kv in vars) text = text.Replace($"{{{{{kv.Key}}}}}", kv.Value);
        return text;
    }

    private async Task DeliverAsync(NotificationTemplateChannel channel, string recipient, string subject, string body, string templateName, Dictionary<string, string>? variables, CancellationToken ct)
    {
        switch (channel)
        {
            case NotificationTemplateChannel.Email:
                await SendEmailAsync(recipient, subject, body, ct);
                break;
            case NotificationTemplateChannel.SMS:
                await SendSmsAsync(recipient, body, ct);
                break;
            case NotificationTemplateChannel.WhatsApp:
                await SendWhatsAppAsync(recipient, templateName, variables ?? new(), ct);
                break;
            case NotificationTemplateChannel.InApp:
                break;
            default:
                throw new NotSupportedException($"{channel} delivery is not configured.");
        }
    }

    private async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        var host = _configuration["Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("SMTP host is not configured.");

        using var client = new SmtpClient(host, int.TryParse(_configuration["Email:SmtpPort"], out var port) ? port : 587)
        {
            EnableSsl = bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) ? ssl : true
        };

        var user = _configuration["Email:Username"];
        var pass = _configuration["Email:Password"];
        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass)) client.Credentials = new NetworkCredential(user, pass);

        using var message = new MailMessage(_configuration["Email:FromAddress"] ?? "no-reply@aegiscore.local", to, subject, body) { IsBodyHtml = true };
        await client.SendMailAsync(message, ct);
    }

    private async Task SendSmsAsync(string to, string body, CancellationToken ct)
    {
        var endpoint = _configuration["Sms:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint)) throw new InvalidOperationException("SMS endpoint is not configured.");

        var response = await _httpClientFactory.CreateClient("sms").PostAsJsonAsync(endpoint, new { to, message = body }, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task SendWhatsAppAsync(string to, string templateName, Dictionary<string, string> variables, CancellationToken ct)
    {
        var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
        var token = _configuration["WhatsApp:AccessToken"];
        if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("WhatsApp credentials are not configured.");

        var client = _httpClientFactory.CreateClient("whatsapp");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var payload = new
        {
            messaging_product = "whatsapp",
            to,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = _configuration["WhatsApp:Language"] ?? "en" },
                components = new[] { new { type = "body", parameters = variables.Values.Select(v => new { type = "text", text = v }).ToArray() } }
            }
        };

        var graphVersion = _configuration["WhatsApp:GraphVersion"] ?? "v21.0";
        var response = await client.PostAsJsonAsync($"{graphVersion}/{phoneNumberId}/messages", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    private static NotificationDeliveryDto ToDto(NotificationDelivery d) => new() { Id = d.Id, Channel = d.Channel.ToString(), Recipient = d.Recipient, Status = d.Status.ToString(), SentAt = d.SentAt };
}

public class NoopNotificationRealtimePublisher : INotificationRealtimePublisher
{
    public Task PublishAsync(string recipient, NotificationDeliveryDto delivery, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class GrievanceService : IGrievanceService
{
    private readonly AegisDbContext _db;
    public GrievanceService(AegisDbContext db) => _db = db;

    public async Task<GrievanceTicketDto> CreateAsync(CreateGrievanceTicketRequest request, Guid submitterId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Subject)) throw new ArgumentException("Subject is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Description)) throw new ArgumentException("Description is required.", nameof(request));

        var ticket = new GrievanceTicket
        {
            Id = Guid.NewGuid(),
            TicketNumber = $"GRV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            SubmitterId = submitterId,
            Category = request.Category,
            Subject = request.Subject,
            Description = request.Description,
            Priority = request.Priority
        };
        _db.GrievanceTickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return ToDto(ticket);
    }

    public async Task<GrievanceTicketDto> RespondAsync(Guid ticketId, GrievanceResponseRequest request, Guid responderId, CancellationToken ct = default)
    {
        var ticket = await _db.GrievanceTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct)
            ?? throw new InvalidOperationException("Grievance ticket not found.");
        var response = new GrievanceResponse { Id = Guid.NewGuid(), TicketId = ticketId, ResponderId = responderId, ResponseText = request.ResponseText, StatusUpdate = request.StatusUpdate };
        _db.GrievanceResponses.Add(response);
        if (!string.IsNullOrWhiteSpace(request.StatusUpdate)) ticket.Status = request.StatusUpdate;
        if (string.Equals(ticket.Status, "resolved", StringComparison.OrdinalIgnoreCase) || string.Equals(ticket.Status, "closed", StringComparison.OrdinalIgnoreCase)) ticket.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(ticket);
    }

    public async Task<List<GrievanceTicketDto>> ListAsync(Guid? submitterId = null, CancellationToken ct = default)
    {
        var query = _db.GrievanceTickets.AsNoTracking();
        if (submitterId.HasValue) query = query.Where(t => t.SubmitterId == submitterId.Value);
        return await query.OrderByDescending(t => t.SubmittedAt).Select(t => ToDto(t)).ToListAsync(ct);
    }

    private static GrievanceTicketDto ToDto(GrievanceTicket t) => new() { Id = t.Id, TicketNumber = t.TicketNumber, Category = t.Category, Subject = t.Subject, Status = t.Status, Priority = t.Priority, SubmittedAt = t.SubmittedAt, ResolvedAt = t.ResolvedAt };
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
        catch (Exception ex) { exec.Status = JobExecutionStatus.Failed; exec.ErrorMessage = ex.Message; exec.CompletedAt = DateTime.UtcNow; }
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
