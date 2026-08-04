using AegisCore.Application.Finance;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Finance;

public static class DefaultPredictionServiceExtensions
{
    public static IServiceCollection AddDefaultPrediction(this IServiceCollection services)
    {
        services.AddScoped<IDefaultPredictionService, DefaultPredictionService>();
        return services;
    }
}
