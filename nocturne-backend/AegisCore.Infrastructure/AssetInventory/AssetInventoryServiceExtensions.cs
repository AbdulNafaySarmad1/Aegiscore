using AegisCore.Application.AssetInventory;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.AssetInventory;

public static class AssetInventoryServiceExtensions
{
    public static IServiceCollection AddAssetInventory(this IServiceCollection services)
    {
        services.AddScoped<IAssetInventoryService, AssetInventoryService>();
        return services;
    }
}
