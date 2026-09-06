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

        services.AddHttpContextAccessor();
        services.AddSingleton<IDbDialectAdapter>(adapter);
        adapter.AddDbProvider(services, connectionString);
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<AegisDbContext>());

        return services;
    }
}
