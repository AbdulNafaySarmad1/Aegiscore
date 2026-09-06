using AegisCore.Infrastructure.Database;
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

        var adapter = DbDialectAdapterResolver.FromConfiguration(configuration);
        var connectionString = DbConnectionStringFactory.Build(configuration, adapter);
        var optionsBuilder = new DbContextOptionsBuilder<AegisDbContext>();
        adapter.ConfigureOptions(optionsBuilder, connectionString);
        return new AegisDbContext(optionsBuilder.Options, dialectAdapter: adapter);
    }
}
