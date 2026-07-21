using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Infrastructure.Repositories;
using AegisCore.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Security;

public static class LoginAuditServiceExtensions
{
    public static IServiceCollection AddLoginAudit(this IServiceCollection services)
    {
        services.TryAddScoped<ILoginAuditRecordRepository, LoginAuditRecordRepository>();
        services.TryAddSingleton<IGeoIpService, GeoIpService>();
        services.TryAddSingleton<LoginAuditService>();
        services.TryAddSingleton<ILoginAuditService>(sp => sp.GetRequiredService<LoginAuditService>());
        services.AddHostedService<LoginAuditArchiveWorker>();
        return services;
    }
}
