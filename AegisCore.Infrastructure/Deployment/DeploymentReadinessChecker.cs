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
    private readonly IEnvironmentValidator _environmentValidator;
    private readonly IHostEnvironment _environment;
    private readonly List<ReadinessCheck> _checks = new();

    public bool AllPassed => _checks.All(c => c.Passed);

    public DeploymentReadinessChecker(
        IConfiguration configuration,
        ILogger<DeploymentReadinessChecker> logger,
        IEnvironmentValidator environmentValidator,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _environmentValidator = environmentValidator;
        _environment = environment;
    }

    public async Task<IReadOnlyList<ReadinessCheck>> RunAllAsync(CancellationToken ct = default)
    {
        _checks.Clear();

        CheckRequiredEnvVars();
        await CheckPostgresAsync(ct);
        await CheckRedisAsync(ct);
        CheckJwtConfig();
        CheckCloudProvider();

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
            var cs = new[]
                {
                    _configuration["AEGISCORE_DB_CONNECTION"],
                    _configuration["POSTGRES_CONNECTION_STRING"],
                    _configuration.GetConnectionString("Postgres"),
                    _configuration.GetConnectionString("SupabasePostgres"),
                    _configuration["Postgres:ConnectionString"],
                    _configuration["Database:ConnectionString"]
                }
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
                ?? BuildConnectionStringFromRegistry();

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
                _checks.Add(new ReadinessCheck("PostgreSQL Application Role", true, "The connected database role could not be inspected."));
            else if (reader.GetBoolean(0) || reader.GetBoolean(1))
                _checks.Add(new ReadinessCheck("PostgreSQL Application Role", true, "The API database role must not be SUPERUSER or BYPASSRLS."));
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
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
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
        var result = _environmentValidator.Validate();
        var missing = result.MissingRequired.Concat(result.MissingConditional).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        if (result.IsValid)
            _checks.Add(new ReadinessCheck(name, true, $"All {EnvironmentValidator.Registry.Count} registered variables satisfy their requirements."));
        else
            _checks.Add(new ReadinessCheck(name, false,
                $"Missing: {string.Join(", ", missing)}. Invalid: {string.Join(" ", result.Errors)}"));

        foreach (var variable in missing)
            _logger.LogCritical("STARTUP BLOCKED: Required environment variable {Name} is not set. Set it before starting AegisCore.", variable);
        foreach (var error in result.Errors)
            _logger.LogCritical("STARTUP BLOCKED: {Error}", error);

        if (!result.IsValid && FailOnReadinessError())
            throw new ApplicationException("STARTUP BLOCKED: Required environment configuration is missing or invalid.");
    }

    private bool FailOnReadinessError()
        => _configuration.GetValue<bool?>("Deployment:FailOnReadinessError") ?? _environment.IsProduction();

    private string? BuildConnectionStringFromRegistry()
    {
        var host = _configuration["DB_HOST"];
        var port = _configuration["DB_PORT"];
        var database = _configuration["DB_NAME"];
        var user = _configuration["DB_USER"];
        var password = _configuration["DB_PASS"];
        if (new[] { host, port, database, user, password }.Any(string.IsNullOrWhiteSpace)) return null;
        return $"Host={host};Port={port};Database={database};Username={user};Password={password};SslMode=Require";
    }
}

public record ReadinessCheck(string Name, bool Passed, string Detail);

public static class DeploymentReadinessServiceExtensions
{
    public static IServiceCollection AddDeploymentReadiness(this IServiceCollection services)
    {
        services.AddSingleton<IEnvironmentValidator, EnvironmentValidator>();
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
            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var failOnError = configuration.GetValue<bool?>("Deployment:FailOnReadinessError") ?? environment.IsProduction();
            if (failOnError)
            {
                logger.LogCritical("Deployment readiness check FAILED. Strict startup is enabled.");
                throw new ApplicationException("Deployment readiness checks failed.");
            }

            logger.LogCritical("Deployment readiness check FAILED. Application will continue in degraded mode.");
        }
    }
}
