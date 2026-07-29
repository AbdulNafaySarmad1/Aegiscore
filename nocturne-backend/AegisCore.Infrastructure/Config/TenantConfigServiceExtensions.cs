using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Config;

public static class TenantConfigServiceExtensions
{
    public static IServiceCollection AddTenantConfig(this IServiceCollection services)
    {
        services.TryAddSingleton<TenantConfigService>();
        return services;
    }
}
