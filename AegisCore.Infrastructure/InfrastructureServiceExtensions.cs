using AegisCore.Infrastructure.AI;
using AegisCore.Infrastructure.Admissions;
using AegisCore.Infrastructure.ApiGateway;
using AegisCore.Infrastructure.AssetInventory;
using AegisCore.Infrastructure.Backup;
using AegisCore.Infrastructure.Campus;
using AegisCore.Infrastructure.CampusOperations;
using AegisCore.Infrastructure.Cloud;
using AegisCore.Infrastructure.Compliance;
using AegisCore.Infrastructure.Config;
using AegisCore.Infrastructure.ConfigCenter;
using AegisCore.Infrastructure.CoreERP;
using AegisCore.Infrastructure.Curriculum;
using AegisCore.Infrastructure.DegreeAudit;
using AegisCore.Infrastructure.Deployment;
using AegisCore.Infrastructure.DigitalTwin;
using AegisCore.Infrastructure.Examination;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Forms;
using AegisCore.Infrastructure.Foundation;
using AegisCore.Infrastructure.Governance;
using AegisCore.Infrastructure.Hr;
using AegisCore.Infrastructure.I18n;
using AegisCore.Infrastructure.Identity;
using AegisCore.Infrastructure.Licensing;
using AegisCore.Infrastructure.Marketplace;
using AegisCore.Infrastructure.MicrosoftEcosystem;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Obe;
using AegisCore.Infrastructure.DataImport;
using AegisCore.Infrastructure.Platform;
using AegisCore.Infrastructure.RuleEngine;
using AegisCore.Infrastructure.Search;
using AegisCore.Infrastructure.Security;
using AegisCore.Infrastructure.Security.Abac;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Infrastructure.SsoHitl;
using AegisCore.Infrastructure.StudentLifecycle;
using AegisCore.Infrastructure.Support;
using AegisCore.Infrastructure.Workflow;
using AegisCore.Infrastructure.Replication;
using AegisCore.Infrastructure.SyncLag;
using AegisCore.Infrastructure.DocumentVault;
using AegisCore.Infrastructure.ProfilePictures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace AegisCore.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAegisDbContext(configuration);
        services.TryAddScoped(typeof(AegisCore.Contracts.Common.Interfaces.IRepository<>), typeof(Repositories.Repository<>));
        services.TryAddScoped<AegisCore.Contracts.Common.Interfaces.IUnitOfWork, UnitOfWork>();
        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString = FirstConfigured(
                configuration["AEGISCORE_REDIS_CONNECTION"],
                configuration["REDIS_CONNECTION_STRING"],
                configuration.GetConnectionString("Redis"),
                configuration["Redis:ConnectionString"])
                ?? "localhost:6379";
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddStorageProvider(configuration);
        services.AddScoped<AegisCore.Application.Foundation.IPolicyService, PolicyService>();
        services.AddScoped<AegisCore.Application.Foundation.IAuditService, AuditService>();
        services.AddScoped<AegisCore.Application.Foundation.IAcademicCalendarService, AcademicCalendarService>();
        services.AddHttpClient("license");
        services.AddHttpClient("infisical", client =>
        {
            var baseUrl = configuration["INFISICAL_URL"];
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                client.BaseAddress = uri;
        });
        services.AddScoped<AegisCore.Application.Licensing.ILicenseService, AegisCore.Infrastructure.Licensing.LicenseService>();
        services.AddScoped<AegisCore.Application.Admissions.IMeritCalculationService, MeritCalculationService>();
        services.AddScoped<AegisCore.Application.Admissions.IIBCCEquivalenceService, IBCCEquivalenceService>();
        services.AddScoped<AegisCore.Application.Admissions.ISeatAllocationService, SeatAllocationService>();

        // P20 Applicant Portal
        services.AddHttpClient("clip");
        services.AddSingleton<AegisCore.Application.Admissions.IApplicantTenantResolver, ApplicantTenantResolver>();
        services.AddScoped<AegisCore.Application.Admissions.IApplicantIdGenerator, ApplicantIdGenerator>();
        services.AddScoped<AegisCore.Application.Admissions.IApplicantOtpService, ApplicantOtpService>();
        services.AddScoped<AegisCore.Application.Admissions.IApplicantTokenService, ApplicantTokenService>();
        services.AddScoped<AegisCore.Application.Admissions.IPhotoValidationService, ClipPhotoValidationService>();
        services.AddScoped<AegisCore.Application.Admissions.IApplicantPortalService, ApplicantPortalService>();
        services.AddScoped<AegisCore.Application.Admissions.IAdmissionCatalogService, AdmissionCatalogService>();
        services.AddScoped<AegisCore.Application.Admissions.IAdmissionConfigService, AdmissionConfigService>();
        services.AddScoped<AegisCore.Application.Admissions.IAdmissionsAdminService, AdmissionsAdminService>();

        // P21 AI Test Engine
        services.AddScoped<AegisCore.Application.Admissions.ISyllabusOcrService, SyllabusOcrService>();
        services.AddScoped<AegisCore.Application.Admissions.IAdmissionQuestionGenerator, AdmissionQuestionGenerator>();
        services.AddScoped<AegisCore.Application.Admissions.IAdmissionTestConfigService, AdmissionTestConfigService>();
        services.AddScoped<AegisCore.Application.Admissions.IQuestionBankService, QuestionBankService>();
        services.AddScoped<AegisCore.Application.Admissions.ITestSchedulingService, TestSchedulingService>();
        services.AddScoped<AegisCore.Application.Admissions.ITestTerminalService, TestTerminalService>();
        services.AddScoped<AegisCore.Application.Admissions.ITestProctorService, TestProctorService>();
        services.AddScoped<AegisCore.Application.Examination.IGradingService, GradingService>();
        services.AddScoped<AegisCore.Application.Examination.ITranscriptService, TranscriptService>();
        services.AddScoped<AegisCore.Application.Obe.ICisExtractionService, CisExtractionService>();
        services.AddScoped<AegisCore.Application.Obe.IHemisExportService, HemisExportService>();
        services.AddScoped<AegisCore.Application.Obe.IOBEAttainmentService, OBEAttainmentService>();
        services.AddScoped<AegisCore.Application.Hr.IPayrollService, PayrollService>();
        services.AddScoped<AegisCore.Application.Finance.IFeeService, FeeService>();
        services.AddScoped<AegisCore.Application.Finance.IAccountingService, AccountingService>();
        services.AddScoped<AegisCore.Application.Finance.IBudgetService, BudgetService>();
        services.AddCloudProvider(configuration);
        services.AddPqcCryptography();
        services.AddSingleton<IHybridPqcReplayStore, RedisHybridPqcReplayStore>();
        services.AddScoped<IPqcSignatureService, PqcSignatureService>();
        services.AddComplianceCenter(configuration);
        services.AddLoginAudit();
        services.AddPlatformServices(configuration);
        services.AddDisasterRecovery(configuration);
        services.AddExtendedPlatform();
        services.AddDataImport();
        services.AddReplication();
        services.AddSyncLag();
        services.AddDocumentVault();
        services.AddProfilePictures();
        services.AddRuleEngine(configuration);
        services.AddSystemAlerting();
        services.AddCoreErpModules();
        services.AddFormsBuilder(configuration);
        services.AddDeploymentReadiness();
        services.AddTenantConfig();
        services.AddMarketplace(configuration);
        services.AddMultiCampus(configuration);
        services.AddCampusOperations();
        services.AddWorkflowStudio(configuration);
        services.AddInternationalization(configuration);
        services.AddConfigCenter(configuration);
        services.AddIdentityGovernance(configuration);
        services.AddAbacAuthorization();
        services.AddCasbinAuthorization();
        services.AddApiGateway(configuration);
        services.AddDegreeAudit(configuration);
        services.AddEnterpriseSearch(configuration);
        services.AddDigitalTwin(configuration);
        services.AddOnboardingWizard();
        services.AddAiPlatform(configuration);
        services.AddScoped<AzureDocumentIntelligenceService>();
        services.AddMicrosoftEcosystem(configuration);
        services.AddStudentLifecycle();
        services.AddAssetInventory();
        services.AddGovernance();
        services.AddCurriculumManagement();
        services.AddSsoHitl();
        services.AddAiSupportTicketing();
        services.AddDefaultPrediction();
        services.AddVisitingFaculty();
        services.AddAccreditation();
        services.AddBoardPortal();
        services.AddVoiceAssistant();
        services.AddGamification();
        services.AddMentalHealth();
        services.AddForeignStudent();
        services.AddElections();
        services.AddCafeteria();
        services.AddTransport();
        return services;
    }

    private static string? FirstConfigured(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
