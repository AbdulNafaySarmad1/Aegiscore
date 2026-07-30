using AegisCore.Application.Finance;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Finance;

public static class FinanceModuleServiceExtensions
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        services.AddScoped<IFinanceModuleService, FinanceModuleService>();
        services.AddScoped<FinanceAuditSealService>();
        services.AddHostedService<FinanceAuditSealWorker>();
        return services;
    }
}
