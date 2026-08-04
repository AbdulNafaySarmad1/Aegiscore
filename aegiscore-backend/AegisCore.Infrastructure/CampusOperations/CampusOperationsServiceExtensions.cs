using AegisCore.Application.CampusOperations;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.CampusOperations;

public static class CampusOperationsServiceExtensions
{
    public static IServiceCollection AddCampusOperations(this IServiceCollection services)
    {
        services.AddScoped<ICampusOperationsService, CampusOperationsService>();
        return services;
    }
}
