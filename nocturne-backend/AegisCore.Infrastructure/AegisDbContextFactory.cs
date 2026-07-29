using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure;

public sealed class AegisDbContextFactory : IDesignTimeDbContextFactory<AegisDbContext>
{
    public AegisDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("AegisCore.API/appsettings.json", optional: true)
            .AddJsonFile("AegisCore.API/appsettings.Development.json", optional: true)
            .AddInMemoryCollection(Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(entry => (string)entry.Key, entry => (string?)entry.Value))
            .Build();

        var connectionString = BuildConnectionString(configuration);

        var options = new DbContextOptionsBuilder<AegisDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(AegisDbContext).Assembly.GetName().Name))
            .Options;

        return new AegisDbContext(options);
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        var explicitCs = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings:Default"]
            ?? configuration["Database:ConnectionString"]
            ?? configuration["NOCTURNE_DB_CONNECTION"]
            ?? configuration["POSTGRES_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(explicitCs))
            return explicitCs;

        var host = configuration["DB_HOST"] ?? configuration["POSTGRES_HOST"] ?? "localhost";
        var port = configuration["DB_PORT"] ?? configuration["POSTGRES_PORT"] ?? "5432";
        var db = configuration["DB_NAME"] ?? configuration["POSTGRES_DB"] ?? "aegiscore";
        var user = configuration["DB_USERNAME"] ?? configuration["POSTGRES_USER"] ?? "aegiscore";
        var password = configuration["DB_PASSWORD"] ?? configuration["POSTGRES_PASSWORD"] ?? "";
        var ssl = (configuration["DB_SSL"] ?? "Disable").Equals("Disable", StringComparison.OrdinalIgnoreCase)
            ? "Disable" : "Require";

        return $"Host={host};Port={port};Database={db};Username={user};Password={password};SslMode={ssl}";
    }
}
