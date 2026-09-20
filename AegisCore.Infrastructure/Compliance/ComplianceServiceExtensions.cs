using AegisCore.Application.Compliance;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Compliance;

public static class ComplianceServiceExtensions
{
    public static IServiceCollection AddComplianceCenter(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IComplianceFrameworkRepository, ComplianceFrameworkRepository>();
        services.TryAddScoped<IComplianceControlRepository, ComplianceControlRepository>();
        services.TryAddScoped<IAuditFindingRepository, AuditFindingRepository>();
        services.TryAddScoped<ICorrectiveActionPlanRepository, CorrectiveActionPlanRepository>();

        services.TryAddScoped<IComplianceService, ComplianceService>();
        services.TryAddScoped<IFerpaAuditService, FerpaAuditService>();
        services.TryAddScoped<IAccessAuditService, AccessAuditService>();
        services.TryAddScoped<IPrivacyComplianceService, PrivacyComplianceService>();
        services.AddHostedService<ComplianceRetentionWorker>();

        return services;
    }
}
