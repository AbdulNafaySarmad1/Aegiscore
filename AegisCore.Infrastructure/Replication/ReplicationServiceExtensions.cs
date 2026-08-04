using AegisCore.Application.Replication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Replication;

public static class ReplicationServiceExtensions
{
    public static IServiceCollection AddReplication(this IServiceCollection services)
    {
        services.TryAddScoped<IReplicaManagerService, ReplicaManagerService>();
        return services;
    }
}
