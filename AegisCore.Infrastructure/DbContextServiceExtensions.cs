using AegisCore.Infrastructure.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure;

public static class DbContextServiceExtensions
{
    public static IServiceCollection AddAegisDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = BuildConnectionString(configuration);
        services.AddHttpContextAccessor();
        services.AddSingleton<TenantSessionInterceptor>();

        services.AddDbContext<AegisDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AegisDbContext).Assembly.GetName().Name);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            }).AddInterceptors(serviceProvider.GetRequiredService<TenantSessionInterceptor>()));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<AegisDbContext>());

        return services;
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        var explicitCs = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings:Default"]
            ?? configuration["Database:ConnectionString"]
            ?? configuration["AEGISCORE_DB_CONNECTION"]
            ?? configuration["POSTGRES_CONNECTION_STRING"];
        if (IsConfigured(explicitCs))
            return explicitCs!;

        var host = configuration["DB_HOST"] ?? configuration["POSTGRES_HOST"] ?? "localhost";
        var port = configuration["DB_PORT"] ?? configuration["POSTGRES_PORT"] ?? "5432";
        var db = configuration["DB_NAME"] ?? configuration["POSTGRES_DB"] ?? "aegiscore";
        var user = configuration["DB_USERNAME"] ?? configuration["POSTGRES_USER"] ?? "aegiscore";
        var password = configuration["DB_PASSWORD"] ?? configuration["POSTGRES_PASSWORD"] ?? "";
        var ssl = (configuration["DB_SSL"] ?? "Require").Equals("Disable", StringComparison.OrdinalIgnoreCase)
            ? "Disable" : "Require";

        var pool = configuration.GetValue<int?>("DB_POOL_MAX") ?? 200;
        var timeout = configuration.GetValue<int?>("DB_COMMAND_TIMEOUT") ?? 30;
        return $"Host={host};Port={port};Database={db};Username={user};Password={password};SslMode={ssl};MaxPoolSize={pool};CommandTimeout={timeout};Pooling=true;ConnectionIdleLifetime=300;ConnectionPruningInterval=10";
    }

    private static bool IsConfigured(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.TrimStart().StartsWith("${", StringComparison.Ordinal);
}
