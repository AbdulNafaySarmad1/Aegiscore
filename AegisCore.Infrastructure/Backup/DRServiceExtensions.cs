using AegisCore.Application.Backup;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quartz;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Backup;

public static class DRServiceExtensions
{
    public static IServiceCollection AddDisasterRecovery(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IBackupConfigurationRepository, BackupConfigurationRepository>();
        services.TryAddScoped<IBackupRecordRepository, BackupRecordRepository>();
        services.TryAddScoped<IReadReplicaConfigRepository, ReadReplicaConfigRepository>();

        services.TryAddScoped<IBackupService, BackupService>();
        services.TryAddScoped<IDatabaseHealthService, DatabaseHealthService>();

        services.TryAddTransient<AutomatedBackupJob>();

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("automated-backup");
            q.AddJob<AutomatedBackupJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("backup-hourly-trigger")
                .WithCronSchedule("0 0 * * * ?")
                .WithDescription("Hourly backup check trigger"));
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database_primary", tags: new[] { "database", "primary" })
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache" });

        return services;
    }
}

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDatabaseHealthService _healthService;

    public DatabaseHealthCheck(IDatabaseHealthService healthService)
    {
        _healthService = healthService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var isHealthy = await _healthService.IsPrimaryHealthyAsync(cancellationToken);
        return isHealthy
            ? HealthCheckResult.Healthy("Primary database is healthy.")
            : HealthCheckResult.Unhealthy("Primary database is unreachable.");
    }
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable.", ex);
        }
    }
}
