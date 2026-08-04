using AegisCore.Application.CampusOperations;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.CampusOperations;

public static class CampusOperationsModuleExtensions
{
    public static IServiceCollection AddElections(this IServiceCollection services)
    {
        services.AddScoped<IElectionService, ElectionService>();
        return services;
    }
}
