using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Marketplace;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Marketplace;

public static class MarketplaceServiceExtensions
{
    public static IServiceCollection AddMarketplace(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IExtensionRepository, ExtensionRepository>();
        services.TryAddScoped<IExtensionInstallationRepository, ExtensionInstallationRepository>();
        services.TryAddScoped<IExtensionReviewRepository, ExtensionReviewRepository>();
        services.TryAddScoped<IExtensionService, ExtensionService>();

        return services;
    }
}
