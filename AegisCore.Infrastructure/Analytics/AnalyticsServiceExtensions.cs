using AegisCore.Application.Analytics;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Analytics;

public static class AnalyticsServiceExtensions
{
    public static IServiceCollection AddAnalyticsIntelligence(this IServiceCollection services)
    {
        services.AddSingleton<IStatisticalAnalyticsEngine, StatisticalAnalyticsEngine>();
        services.AddSingleton<ITelemetryPolicyEngine, TelemetryPolicyEngine>();
        services.AddScoped<IAnalyticsIntelligenceService, AnalyticsIntelligenceService>();
        return services;
    }
}
