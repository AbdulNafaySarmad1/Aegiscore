using AegisCore.Application.Common.Interfaces;
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
        services.TryAddScoped<IGeoIpService, GeoIpService>();
        services.TryAddScoped<ILoginAuditService, LoginAuditService>();
        services.TryAddSingleton<IBruteForceProtectionService, BruteForceProtectionService>();
        return services;
    }
}
