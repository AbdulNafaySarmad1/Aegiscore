using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Platform;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Platform;

public static class ExtendedPlatformServiceExtensions
{
    public static IServiceCollection AddExtendedPlatform(this IServiceCollection services)
    {
        services.TryAddScoped<IQueueDefinitionRepository, QueueDefinitionRepository>();
        services.TryAddScoped<IQueueJobRepository, QueueJobRepository>();
        services.TryAddScoped<IDomainEventRepository, DomainEventRepository>();
        services.TryAddScoped<IEventSchemaVersionRepository, EventSchemaVersionRepository>();
        services.TryAddScoped<IDiagnosticFindingRepository, DiagnosticFindingRepository>();
        services.TryAddScoped<ISlowQueryRepository, SlowQueryRepository>();
        services.TryAddScoped<IServiceRegistrationRepository, ServiceRegistrationRepository>();
        services.TryAddScoped<IApiEndpointRepository, ApiEndpointRepository>();
        services.TryAddScoped<IHealthScoreRepository, HealthScoreRepository>();
        services.TryAddScoped<IIntegrationRegistrationRepository, IntegrationRegistrationRepository>();
        services.TryAddScoped<IBenchmarkRunRepository, BenchmarkRunRepository>();
        services.TryAddScoped<IAIProviderRepository, AIProviderRepository>();
        services.TryAddScoped<IAIModelRepository, AIModelRepository>();
        services.TryAddScoped<IAITokenUsageRepository, AITokenUsageRepository>();
        services.TryAddScoped<IPromptRepository, PromptRepository>();
        services.TryAddScoped<IPromptVersionRepository, PromptVersionRepository>();
        services.TryAddScoped<IGuardrailRuleRepository, GuardrailRuleRepository>();
        services.TryAddScoped<IGuardrailViolationRepository, GuardrailViolationRepository>();
        services.TryAddScoped<IAIFeedbackRepository, AIFeedbackRepository>();
        services.TryAddScoped<ILicenseRepository, LicenseRepository>();
        services.TryAddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.TryAddScoped<IUpdateDeploymentRepository, UpdateDeploymentRepository>();
        services.TryAddScoped<IOfficialModuleRepository, OfficialModuleRepository>();
        services.TryAddScoped<IFeeDefaulterRepository, FeeDefaulterRepository>();
        services.TryAddScoped<IMisconductCaseRepository, MisconductCaseRepository>();
        services.TryAddScoped<IFacultyWorkloadRepository, FacultyWorkloadRepository>();
        services.TryAddScoped<IDegreeVerificationRepository, DegreeVerificationRepository>();
        services.TryAddScoped<IRegistrationWindowRepository, RegistrationWindowRepository>();
        services.TryAddScoped<IRegistrationEnrollmentRepository, RegistrationEnrollmentRepository>();
        services.TryAddScoped<ICurriculumProposalRepository, CurriculumProposalRepository>();
        services.TryAddScoped<IAccessAuditLogRepository, AccessAuditLogRepository>();
        services.TryAddScoped<IHardcodingFindingRepository, HardcodingFindingRepository>();

        services.TryAddScoped<IQueueManagementService, QueueManagementService>();
        services.TryAddScoped<IEventExplorerService, EventExplorerService>();
        services.TryAddScoped<IDiagnosticsService, DiagnosticsService>();
        services.TryAddScoped<IArchitectureExplorerService, ArchitectureExplorerService>();
        services.TryAddScoped<IHealthAnalyzerService, HealthAnalyzerService>();
        services.TryAddScoped<IIntegrationMonitoringService, IntegrationMonitoringService>();
        services.TryAddScoped<IBenchmarkService, BenchmarkService>();
        services.TryAddScoped<IAIOperationsService, AIOperationsService>();
        services.TryAddScoped<IPromptManagementService, PromptManagementService>();
        services.TryAddScoped<IGuardrailService, GuardrailService>();
        services.TryAddScoped<IAIAnalyticsService, AIAnalyticsService>();
        services.TryAddScoped<ILicenseService, LicenseService>();
        services.TryAddScoped<ICustomerPortalService, CustomerPortalService>();
        services.TryAddScoped<IUpdateService, UpdateService>();
        services.TryAddScoped<IOfficialMarketplaceService, OfficialMarketplaceService>();
        services.TryAddScoped<IFeeDefaulterService, FeeDefaulterService>();
        services.TryAddScoped<IMisconductService, MisconductService>();
        services.TryAddScoped<IFacultyWorkloadService, FacultyWorkloadService>();
        services.TryAddScoped<IDegreeVerificationService, DegreeVerificationService>();
        services.TryAddScoped<IRegistrationService, RegistrationService>();
        services.TryAddScoped<ICurriculumGovernanceService, CurriculumGovernanceService>();
        services.TryAddScoped<IZeroTrustService, ZeroTrustService>();
        services.TryAddScoped<IHardcodingAuditService, HardcodingAuditService>();

        // P111-P127 Extended Platform Services
        services.TryAddScoped<IGradeAppealService, GradeAppealService>();
        services.TryAddScoped<IGradeDistributionService, GradeDistributionService>();
        services.TryAddScoped<IPlagiarismDetectionService, PlagiarismDetectionService>();
        services.TryAddScoped<IPlagiarismApiService, PlagiarismApiService>();
        services.TryAddScoped<IConvocationService, ConvocationService>();
        services.TryAddScoped<ISemesterFreezeService, SemesterFreezeService>();
        services.TryAddScoped<IProctoringService, ProctoringService>();
        services.TryAddScoped<IPrerequisiteConflictService, PrerequisiteConflictService>();
        services.TryAddScoped<IDefaulterPredictionService, DefaulterPredictionService>();
        services.TryAddScoped<IVisitingFacultyService, VisitingFacultyService>();
        services.TryAddScoped<IReadinessMeterService, ReadinessMeterService>();
        services.TryAddScoped<IAchievementService, AchievementService>();
        services.TryAddScoped<IMentalHealthService, MentalHealthService>();
        services.TryAddScoped<IForeignStudentService, ForeignStudentService>();
        services.TryAddScoped<IVoiceAssistantService, VoiceAssistantService>();
        services.TryAddScoped<IBoardPortalService, BoardPortalService>();
        services.TryAddScoped<IElectionService, ElectionService>();
        services.TryAddScoped<ICatalogIngestionService, CatalogIngestionService>();
        services.TryAddScoped<IClassGenerationService, ClassGenerationService>();

        return services;
    }
}
