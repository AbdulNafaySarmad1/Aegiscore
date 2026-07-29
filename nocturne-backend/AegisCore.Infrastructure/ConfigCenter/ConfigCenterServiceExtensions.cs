using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.ConfigCenter;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.ConfigCenter;

public static class ConfigCenterServiceExtensions
{
    public static IServiceCollection AddConfigCenter(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IConfigurationEntryRepository, ConfigurationEntryRepository>();
        services.TryAddScoped<IConfigVersionRepository, ConfigVersionRepository>();
        services.TryAddScoped<IConfigAuditLogRepository, ConfigAuditLogRepository>();

        services.TryAddScoped<IConfigurationCenterService, ConfigurationCenterService>();

        return services;
    }
}
