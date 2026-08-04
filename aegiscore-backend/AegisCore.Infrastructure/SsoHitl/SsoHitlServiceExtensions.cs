using AegisCore.Application.SsoHitl;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.SsoHitl;

public static class SsoHitlServiceExtensions
{
    public static IServiceCollection AddSsoHitl(this IServiceCollection services)
    {
        services.AddScoped<IMultiTenantSsoService, MultiTenantSsoService>();
        services.AddSingleton<ITwoFactorService, TwoFactorService>();
        services.AddSingleton<IAgenticExamService, AgenticExamService>();
        return services;
    }
}
