using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.DigitalTwin;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.DigitalTwin;

public static class DigitalTwinServiceExtensions
{
    public static IServiceCollection AddDigitalTwin(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IIoTSensorRepository, IoTSensorRepository>();
        services.TryAddScoped<ISensorReadingRepository, SensorReadingRepository>();
        services.TryAddScoped<IRoomOccupancyRepository, RoomOccupancyRepository>();
        services.TryAddScoped<IFloorPlanRepository, FloorPlanRepository>();
        services.TryAddScoped<IEnergyReadingRepository, EnergyReadingRepository>();

        services.TryAddScoped<IDigitalTwinService, DigitalTwinService>();

        return services;
    }
}
