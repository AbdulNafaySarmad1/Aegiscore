using AegisCore.Application.Workflow;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Security;

public interface ISecurityAlertService
{
    Task<SecurityAlert?> RaiseAsync(Guid tenantId, string type, string severity, string description, Guid? subjectUserId, int count, CancellationToken ct = default);
}

/// <summary>
/// Records a security alert once per subject per type per window, logs it at warning level, and
/// emails the address in <c>Security:AlertEmail</c> for High and Critical. Deduplication matters
/// because the detectors run on every sign-in: without it a password-spraying run would send a
/// hundred identical emails and the real signal would be lost. Compliance control AC-54.
/// </summary>
public sealed class SecurityAlertService(
    AegisDbContext db,
    IConfiguration configuration,
    ILogger<SecurityAlertService> logger,
    INotificationService? notifications = null) : ISecurityAlertService
{
    public async Task<SecurityAlert?> RaiseAsync(Guid tenantId, string type, string severity, string description, Guid? subjectUserId, int count, CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty) return null;
        var window = DateTime.UtcNow.AddMinutes(-configuration.GetValue("Security:AlertDeduplicationMinutes", 60));
        var duplicate = await db.SecurityAlerts.IgnoreQueryFilters().AnyAsync(
            a => a.TenantId == tenantId && a.Type == type && a.SubjectUserId == subjectUserId && a.DetectedAt > window, ct);
        if (duplicate) return null;

        var alert = new SecurityAlert
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Type = type, Severity = severity,
            Description = description.Length <= 1000 ? description : description[..1000],
            SubjectUserId = subjectUserId, Count = count, DetectedAt = DateTime.UtcNow,
        };
        db.SecurityAlerts.Add(alert);
        await db.SaveChangesAsync(ct);

        logger.LogWarning("Security alert [{Severity}] {Type}: {Description}", severity, type, alert.Description);

        var recipient = configuration["Security:AlertEmail"];
        if (notifications is not null && !string.IsNullOrWhiteSpace(recipient) && severity is "High" or "Critical")
        {
            try
            {
                await notifications.SendEmailAsync(recipient, $"AegisCore security alert: {type}",
                    $"<p><strong>{severity}</strong> — {System.Net.WebUtility.HtmlEncode(alert.Description)}</p><p>Detected {alert.DetectedAt:u}. Follow docs/compliance/11-breach-notification.md.</p>", ct);
            }
            catch (Exception ex) { logger.LogError(ex, "Security alert email to {Recipient} failed", recipient); }
        }
        return alert;
    }
}

/// <summary>
/// Watches the access audit for one account reading or exporting an unusual number of student
/// records in a short window — the shape a bulk extraction takes. Thresholds are per tenant
/// settings so a registrar's legitimate results run does not cry wolf every semester.
/// </summary>
public sealed class BulkAccessAlertWorker(IServiceProvider services, ILogger<BulkAccessAlertWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
                var alerts = scope.ServiceProvider.GetRequiredService<ISecurityAlertService>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var minutes = configuration.GetValue("Security:BulkAccessWindowMinutes", 15);
                var readThreshold = configuration.GetValue("Security:BulkAccessReadThreshold", 200);
                var exportThreshold = configuration.GetValue("Security:BulkAccessExportThreshold", 25);
                var since = DateTime.UtcNow.AddMinutes(-minutes);

                var counts = await db.FerpaAuditLogs.IgnoreQueryFilters().AsNoTracking()
                    .Where(log => log.AccessedAt > since && log.UserId != Guid.Empty)
                    .GroupBy(log => new { log.TenantId, log.UserId, log.Action })
                    .Select(group => new { group.Key.TenantId, group.Key.UserId, group.Key.Action, Count = group.Count() })
                    .ToListAsync(stoppingToken);

                foreach (var row in counts)
                {
                    var isExport = row.Action is "Export" or "Print";
                    var threshold = isExport ? exportThreshold : readThreshold;
                    if (row.Count < threshold) continue;
                    await alerts.RaiseAsync(row.TenantId,
                        isExport ? "BulkExport" : "BulkRecordAccess",
                        isExport ? "High" : "Medium",
                        $"{row.Count} student record {row.Action.ToLowerInvariant()} operations in {minutes} minutes by one account.",
                        row.UserId, row.Count, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Bulk access alert cycle failed"); }

            try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
            catch (OperationCanceledException) { }
        }
    }
}
