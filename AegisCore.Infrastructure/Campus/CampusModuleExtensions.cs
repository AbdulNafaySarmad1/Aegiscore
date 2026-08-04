using AegisCore.Application.Campus;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Campus;

public static class CampusModuleExtensions
{
    public static IServiceCollection AddCafeteria(this IServiceCollection services)
    {
        services.AddScoped<ICafeteriaService, CafeteriaService>();
        return services;
    }

    public static IServiceCollection AddTransport(this IServiceCollection services)
    {
        services.AddScoped<ITransportService, TransportService>();
        return services;
    }
}
