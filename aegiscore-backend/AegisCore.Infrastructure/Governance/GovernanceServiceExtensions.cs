using AegisCore.Application.Governance;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Governance;

public static class GovernanceServiceExtensions
{
    public static IServiceCollection AddGovernance(this IServiceCollection services)
    {
        services.AddScoped<IGovernanceService, GovernanceService>();
        return services;
    }
}
