using AegisCore.Infrastructure.AI;
using AegisCore.Infrastructure.ApiGateway;
using AegisCore.Infrastructure.Backup;
using AegisCore.Infrastructure.Campus;
using AegisCore.Infrastructure.Cloud;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.DegreeAudit;
using AegisCore.Infrastructure.Compliance;
using AegisCore.Infrastructure.Config;
using AegisCore.Infrastructure.ConfigCenter;
using AegisCore.Infrastructure.CoreERP;
using AegisCore.Infrastructure.Deployment;
using AegisCore.Infrastructure.DigitalTwin;
using AegisCore.Infrastructure.Forms;
using AegisCore.Infrastructure.I18n;
using AegisCore.Infrastructure.Identity;
using AegisCore.Infrastructure.Marketplace;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Platform;
using AegisCore.Infrastructure.RuleEngine;
using AegisCore.Infrastructure.Search;
using AegisCore.Infrastructure.Security;
using AegisCore.Infrastructure.Workflow;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAegisDbContext(configuration);
        services.AddCloudProvider(configuration);
        services.AddComplianceCenter(configuration);
        services.AddLoginAudit();
        services.AddPlatformServices(configuration);
        services.AddDisasterRecovery(configuration);
        services.AddExtendedPlatform();
        services.AddRuleEngine(configuration);
        services.AddSystemAlerting();
        services.AddCoreErpModules();
        services.AddFormsBuilder(configuration);
        services.AddDeploymentReadiness();
        services.AddTenantConfig();
        services.AddMarketplace(configuration);
        services.AddMultiCampus(configuration);
        services.AddWorkflowStudio(configuration);
        services.AddInternationalization(configuration);
        services.AddConfigCenter(configuration);
        services.AddIdentityGovernance(configuration);
        services.AddApiGateway(configuration);
        services.AddDegreeAudit(configuration);
        services.AddEnterpriseSearch(configuration);
        services.AddDigitalTwin(configuration);
        services.AddOnboardingWizard();
        services.AddAiPlatform(configuration);

        return services;
    }
}
