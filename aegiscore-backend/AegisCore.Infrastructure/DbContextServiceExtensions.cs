using AegisCore.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure;

public static class DbContextServiceExtensions
{
    public static IServiceCollection AddAegisDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = BuildConnectionString(configuration);

        services.AddDbContext<AegisDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AegisDbContext).Assembly.GetName().Name);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            }));

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
        if (!string.IsNullOrWhiteSpace(explicitCs))
            return explicitCs;

        var host = configuration["DB_HOST"] ?? configuration["POSTGRES_HOST"] ?? "localhost";
        var port = configuration["DB_PORT"] ?? configuration["POSTGRES_PORT"] ?? "5432";
        var db = configuration["DB_NAME"] ?? configuration["POSTGRES_DB"] ?? "aegiscore";
        var user = configuration["DB_USERNAME"] ?? configuration["POSTGRES_USER"] ?? "aegiscore";
        var password = configuration["DB_PASSWORD"] ?? configuration["POSTGRES_PASSWORD"] ?? "";
        var ssl = (configuration["DB_SSL"] ?? "Require").Equals("Disable", StringComparison.OrdinalIgnoreCase)
            ? "Disable" : "Require";

        return $"Host={host};Port={port};Database={db};Username={user};Password={password};SslMode={ssl}";
    }
}
