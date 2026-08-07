using AegisCore.Application.SyncLag;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.SyncLag;

public static class SyncLagServiceExtensions
{
    public static IServiceCollection AddSyncLag(this IServiceCollection services)
    {
        services.TryAddScoped<ISyncLagService, SyncLagService>();
        return services;
    }
}
