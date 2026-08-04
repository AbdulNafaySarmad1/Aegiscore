using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.DegreeAudit;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.DegreeAudit;

public static class DegreeAuditServiceExtensions
{
    public static IServiceCollection AddDegreeAudit(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IDegreeProgramRepository, DegreeProgramRepository>();
        services.TryAddScoped<IDegreeRequirementRepository, DegreeRequirementRepository>();
        services.TryAddScoped<IDegreeAuditRepository, DegreeAuditRepository>();
        services.TryAddScoped<ICreditTransferRepository, CreditTransferRepository>();

        services.TryAddScoped<IDegreeAuditService, DegreeAuditService>();

        return services;
    }
}
