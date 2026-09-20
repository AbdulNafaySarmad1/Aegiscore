using AegisCore.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure;

public static class DbContextServiceExtensions
{
    public static IServiceCollection AddAegisDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var adapter = DbDialectAdapterResolver.FromConfiguration(configuration);
        var connectionString = DbConnectionStringFactory.Build(configuration, adapter);

        // The column cipher shares the installation master key with the secret vault and the audit
        // IP encryption. Without a key (development, tests) columns are stored in clear; production
        // cannot start without it because PqcCryptographyService requires the same variable.
        Security.FieldEncryption.FieldCipher.Configure(
            configuration["AEGISCORE_PQC_KEY_BASE64"] ?? configuration["Pqc:MasterKeyBase64"]);

        services.AddHttpContextAccessor();
        services.AddSingleton<IDbDialectAdapter>(adapter);
        adapter.AddDbProvider(services, connectionString);
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<AegisDbContext>());

        return services;
    }
}
