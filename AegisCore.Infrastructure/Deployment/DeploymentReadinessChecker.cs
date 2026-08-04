using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Deployment;

public class DeploymentReadinessChecker
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeploymentReadinessChecker> _logger;
    private readonly List<ReadinessCheck> _checks = new();

    public bool AllPassed => _checks.All(c => c.Passed);

    public DeploymentReadinessChecker(IConfiguration configuration, ILogger<DeploymentReadinessChecker> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ReadinessCheck>> RunAllAsync(CancellationToken ct = default)
    {
        _checks.Clear();

        await CheckPostgresAsync(ct);
        await CheckRedisAsync(ct);
        CheckJwtConfig();
        CheckCloudProvider();
        CheckRequiredEnvVars();

        var passed = _checks.Count(c => c.Passed);
        var failed = _checks.Count(c => !c.Passed);

        if (failed > 0)
            _logger.LogCritical("Deployment readiness FAILED: {Passed}/{Total} checks passed", passed, _checks.Count);
        else
            _logger.LogInformation("Deployment readiness PASSED: all {Count} checks", _checks.Count);

        return _checks;
    }

    private async Task CheckPostgresAsync(CancellationToken ct)
    {
        var name = "PostgreSQL Connectivity";
        try
        {
            var cs = _configuration["AEGISCORE_DB_CONNECTION"]
                ?? _configuration["POSTGRES_CONNECTION_STRING"]
                ?? _configuration.GetConnectionString("Postgres")
                ?? _configuration.GetConnectionString("SupabasePostgres")
                ?? _configuration["Postgres:ConnectionString"]
                ?? _configuration["Database:ConnectionString"];

            if (string.IsNullOrEmpty(cs))
            {
                _checks.Add(new ReadinessCheck(name, false, "No PostgreSQL connection string configured."));
                return;
            }

            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(ct);

            _checks.Add(new ReadinessCheck(name, true, "Connected successfully."));
            await using var roleCommand = new NpgsqlCommand("SELECT rolsuper, rolbypassrls FROM pg_roles WHERE rolname = current_user", conn);
            await using var reader = await roleCommand.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                _checks.Add(new ReadinessCheck("PostgreSQL Application Role", false, "The connected database role could not be inspected."));
            else if (reader.GetBoolean(0) || reader.GetBoolean(1))
                _checks.Add(new ReadinessCheck("PostgreSQL Application Role", false, "The API database role must not be SUPERUSER or BYPASSRLS."));
            else
                _checks.Add(new ReadinessCheck("PostgreSQL Application Role", true, "The API role cannot bypass row-level security."));
        }
        catch (Exception ex)
        {
            _checks.Add(new ReadinessCheck(name, false, $"Connection failed: {ex.Message}"));
        }
    }

    private async Task CheckRedisAsync(CancellationToken ct)
    {
        var name = "Redis Connectivity";
        try
        {
            var cs = ResolveRedisConnectionString(_configuration);

            var options = ConfigurationOptions.Parse(cs);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;

            await using var redis = await ConnectionMultiplexer.ConnectAsync(options);
            await redis.GetDatabase().PingAsync();
            _checks.Add(new ReadinessCheck(name, true, $"Connected to {DescribeRedisEndpoint(options)}."));
        }
        catch (Exception ex)
        {
            _checks.Add(new ReadinessCheck(name, false, $"Connection failed: {ex.Message}"));
        }
    }

    public static string ResolveRedisConnectionString(IConfiguration configuration)
    {
        return new[]
            {
                configuration["AEGISCORE_REDIS_CONNECTION"],
                configuration["REDIS_CONNECTION_STRING"],
                configuration.GetConnectionString("Redis"),
                configuration["Redis:ConnectionString"]
            }
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value) && !value.TrimStart().StartsWith("${", StringComparison.Ordinal))
            ?? "localhost:6379";
    }

    private static string DescribeRedisEndpoint(ConfigurationOptions options)
    {
        var endpoint = options.EndPoints.FirstOrDefault();
        return endpoint is null ? "configured endpoint" : endpoint.ToString() ?? "configured endpoint";
    }

    private void CheckJwtConfig()
    {
        var name = "JWT Configuration";
        var authority = _configuration["Auth:Authority"];
        var audience = _configuration["Auth:Audience"] ?? "aegiscore-api";
        var symmetricSecret = _configuration["AEGISCORE_JWT_SECRET"] ?? _configuration["Jwt:Secret"];

        if (!string.IsNullOrWhiteSpace(symmetricSecret))
            _checks.Add(new ReadinessCheck(name, true, $"Local signing key configured for audience {audience}."));
        else if (string.IsNullOrEmpty(authority))
            _checks.Add(new ReadinessCheck(name, false, "Configure Auth:Authority or AEGISCORE_JWT_SECRET."));
        else
            _checks.Add(new ReadinessCheck(name, true, $"Authority={authority}, Audience={audience}"));
    }

    private void CheckCloudProvider()
    {
        var name = "Cloud Provider";
        var provider = (_configuration["AEGISCORE_CLOUD_PROVIDER"] ?? "ONPREMISE").ToUpperInvariant();
        var valid = new[] { "AZURE", "AWS", "GCP", "ONPREMISE", "R2" };

        if (valid.Contains(provider))
            _checks.Add(new ReadinessCheck(name, true, $"Provider: {provider}"));
        else
            _checks.Add(new ReadinessCheck(name, false, $"Unknown provider: {provider}. Valid: {string.Join(", ", valid)}"));
    }

    private void CheckRequiredEnvVars()
    {
        var name = "Required Environment Variables";
        var required = new[] { "ASPNETCORE_ENVIRONMENT", "AEGISCORE_CLOUD_PROVIDER" };
        var missing = required.Where(v => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v))).ToList();
        if (string.IsNullOrWhiteSpace(_configuration["TENANT_ID"])
            && string.IsNullOrWhiteSpace(_configuration["AEGISCORE_TENANT_ID"]))
        {
            missing.Add("TENANT_ID");
        }

        if (missing.Count == 0)
            _checks.Add(new ReadinessCheck(name, true, "All required variables present."));
        else
            _checks.Add(new ReadinessCheck(name, false, $"Missing: {string.Join(", ", missing)}"));
    }
}

public record ReadinessCheck(string Name, bool Passed, string Detail);

public static class DeploymentReadinessServiceExtensions
{
    public static IServiceCollection AddDeploymentReadiness(this IServiceCollection services)
    {
        services.AddSingleton<DeploymentReadinessChecker>();
        return services;
    }

    public static async Task RunDeploymentReadinessCheckAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var checker = scope.ServiceProvider.GetRequiredService<DeploymentReadinessChecker>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DeploymentReadinessChecker>>();

        var results = await checker.RunAllAsync();

        foreach (var check in results)
        {
            if (check.Passed)
                logger.LogInformation("  [PASS] {Name}: {Detail}", check.Name, check.Detail);
            else
                logger.LogCritical("  [FAIL] {Name}: {Detail}", check.Name, check.Detail);
        }

        if (!checker.AllPassed)
        {
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            if (configuration.GetValue<bool>("Deployment:FailOnReadinessError"))
            {
                logger.LogCritical("Deployment readiness check FAILED. Strict startup is enabled.");
                throw new InvalidOperationException("Deployment readiness checks failed.");
            }

            logger.LogCritical("Deployment readiness check FAILED. Application will continue in degraded mode.");
        }
    }
}
