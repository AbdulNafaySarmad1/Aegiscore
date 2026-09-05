using System.Collections.Concurrent;
using System.Text.Json;
using AegisCore.Application.Compliance;
using AegisCore.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Compliance;

public sealed class ComplianceRetentionWorker(
    IServiceScopeFactory scopes,
    ILogger<ComplianceRetentionWorker> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, DateOnly> _lastRetentionRun = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        await RunCycleAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunCycleAsync(stoppingToken);
    }

    internal async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IPrivacyComplianceService>();
            var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
            await service.ProcessPendingErasureRequestsAsync(cancellationToken);

            var tenantIds = await db.Users.IgnoreQueryFilters().Select(item => item.TenantId)
                .Union(db.FerpaAuditLogs.IgnoreQueryFilters().Select(item => item.TenantId))
                .Union(db.ConsentLedgers.IgnoreQueryFilters().Select(item => item.TenantId))
                .Union(db.ErasureRequests.IgnoreQueryFilters().Select(item => item.TenantId))
                .Where(tenantId => tenantId != Guid.Empty)
                .Distinct()
                .ToListAsync(cancellationToken);
            var configs = await db.ConfigurationEntries.IgnoreQueryFilters()
                .Where(item => tenantIds.Contains(item.TenantId)
                    && (item.Key == "locale.timezone" || item.Key == "compliance.audit_retention_years"))
                .Select(item => new { item.TenantId, item.Key, item.ValueJson })
                .ToListAsync(cancellationToken);

            var nowUtc = DateTime.UtcNow;
            foreach (var tenantId in tenantIds)
            {
                var timezoneId = ConfigValue(configs.FirstOrDefault(item =>
                    item.TenantId == tenantId && item.Key == "locale.timezone")?.ValueJson)
                    ?? DefaultTenantConfigTemplate.DefaultTimezone;
                TimeZoneInfo timezone;
                try { timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId); }
                catch (TimeZoneNotFoundException) { timezone = TimeZoneInfo.Utc; }
                catch (InvalidTimeZoneException) { timezone = TimeZoneInfo.Utc; }

                var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timezone);
                var localDate = DateOnly.FromDateTime(localNow);
                if (localNow.Hour != 2 || _lastRetentionRun.TryGetValue(tenantId, out var lastRun) && lastRun == localDate)
                    continue;

                var yearsText = ConfigValue(configs.FirstOrDefault(item =>
                    item.TenantId == tenantId && item.Key == "compliance.audit_retention_years")?.ValueJson);
                var years = int.TryParse(yearsText, out var configuredYears)
                    ? configuredYears
                    : DefaultTenantConfigTemplate.DefaultAuditRetentionYears;
                var deleted = await service.ApplyRetentionAsync(tenantId, nowUtc, years, cancellationToken);
                _lastRetentionRun[tenantId] = localDate;
                logger.LogInformation("Applied compliance retention for tenant {TenantId}; removed {DeletedCount} records.", tenantId, deleted);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Compliance erasure/retention cycle failed.");
        }
    }

    private static string? ConfigValue(string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(valueJson);
            return document.RootElement.ValueKind == JsonValueKind.String
                ? document.RootElement.GetString()
                : document.RootElement.ToString();
        }
        catch (JsonException)
        {
            return valueJson;
        }
    }
}
