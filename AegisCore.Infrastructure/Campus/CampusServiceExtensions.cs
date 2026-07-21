using AegisCore.Application.Campus;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Campus;

public static class CampusServiceExtensions
{
    public static IServiceCollection AddMultiCampus(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<ICampusRepository, CampusRepository>();
        services.TryAddScoped<IBuildingRepository, BuildingRepository>();
        services.TryAddScoped<IFloorRepository, FloorRepository>();
        services.TryAddScoped<IRoomRepository, RoomRepository>();
        services.TryAddScoped<ICampusConfigurationRepository, CampusConfigurationRepository>();

        services.TryAddScoped<ICampusService, CampusService>();
        services.TryAddScoped<IConsolidatedReportingService, ConsolidatedReportingService>();

        return services;
    }
}
