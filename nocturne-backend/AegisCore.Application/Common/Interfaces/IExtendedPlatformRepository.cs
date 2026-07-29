using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

// P87
public interface IQueueDefinitionRepository : IRepository<QueueDefinition> { Task<IReadOnlyList<QueueDefinition>> GetByStatusAsync(QueueStatus s, CancellationToken ct); }
public interface IQueueJobRepository : IRepository<QueueJob> { Task<IReadOnlyList<QueueJob>> GetByQueueAsync(Guid qId, CancellationToken ct); Task<IReadOnlyList<QueueJob>> GetFailedAsync(CancellationToken ct); Task<IReadOnlyList<QueueJob>> GetDeadLetterAsync(CancellationToken ct); }
// P88
public interface IDomainEventRepository : IRepository<DomainEvent> { Task<IReadOnlyList<DomainEvent>> GetByAggregateAsync(string aggId, CancellationToken ct); Task<IReadOnlyList<DomainEvent>> GetByCorrelationIdAsync(string cId, CancellationToken ct); Task<IReadOnlyList<DomainEvent>> GetFailedAsync(CancellationToken ct); }
public interface IEventSchemaVersionRepository : IRepository<EventSchemaVersion> { Task<EventSchemaVersion?> GetByTypeAndVersionAsync(string type, int v, CancellationToken ct); }
// P89
public interface IDiagnosticFindingRepository : IRepository<DiagnosticFinding> { Task<IReadOnlyList<DiagnosticFinding>> GetByCategoryAsync(string cat, CancellationToken ct); }
public interface ISlowQueryRepository : IRepository<SlowQuery> { Task<IReadOnlyList<SlowQuery>> GetRecentAsync(int limit, CancellationToken ct); }
// P90
public interface IServiceRegistrationRepository : IRepository<ServiceRegistration> { Task<IReadOnlyList<ServiceRegistration>> GetByStatusAsync(ServiceStatus s, CancellationToken ct); }
public interface IApiEndpointRepository : IRepository<ApiEndpoint> { Task<IReadOnlyList<ApiEndpoint>> GetByModuleAsync(string m, CancellationToken ct); }
// P91
public interface IHealthScoreRepository : IRepository<HealthScore> { Task<HealthScore?> GetLatestAsync(CancellationToken ct); Task<IReadOnlyList<HealthScore>> GetHistoryAsync(int limit, CancellationToken ct); }
// P92
public interface IIntegrationRegistrationRepository : IRepository<IntegrationRegistration> { Task<IReadOnlyList<IntegrationRegistration>> GetByTypeAsync(IntegrationType t, CancellationToken ct); Task<IReadOnlyList<IntegrationRegistration>> GetFailingAsync(CancellationToken ct); }
// P93
public interface IBenchmarkRunRepository : IRepository<BenchmarkRun> { Task<IReadOnlyList<BenchmarkRun>> GetRecentAsync(int limit, CancellationToken ct); }
// P94
public interface IAIProviderRepository : IRepository<AIProvider> { Task<IReadOnlyList<AIProvider>> GetActiveAsync(CancellationToken ct); }
public interface IAIModelRepository : IRepository<AIModel> { Task<IReadOnlyList<AIModel>> GetByProviderAsync(Guid pId, CancellationToken ct); }
public interface IAITokenUsageRepository : IRepository<AITokenUsage> { Task<IReadOnlyList<AITokenUsage>> GetByDomainAsync(string domain, DateTime? from, DateTime? to, CancellationToken ct); Task<long> GetTotalTokensAsync(string domain, DateTime? from, DateTime? to, CancellationToken ct); }
// P95
public interface IPromptRepository : IRepository<Prompt> { Task<IReadOnlyList<Prompt>> GetByDomainAsync(string d, CancellationToken ct); Task<Prompt?> GetByNameAsync(string n, CancellationToken ct); Task<Prompt?> GetActiveByDomainAsync(string d, CancellationToken ct); }
public interface IPromptVersionRepository : IRepository<PromptVersion> { Task<IReadOnlyList<PromptVersion>> GetByPromptAsync(Guid pId, CancellationToken ct); }
// P96
public interface IGuardrailRuleRepository : IRepository<GuardrailRule> { Task<IReadOnlyList<GuardrailRule>> GetActiveAsync(CancellationToken ct); }
public interface IGuardrailViolationRepository : IRepository<GuardrailViolation> { Task<IReadOnlyList<GuardrailViolation>> GetRecentAsync(int limit, CancellationToken ct); }
// P97
public interface IAIFeedbackRepository : IRepository<AIFeedback> { Task<IReadOnlyList<AIFeedback>> GetByDomainAsync(string d, CancellationToken ct); }
// P98
public interface ILicenseRepository : IRepository<License> { Task<License?> GetByKeyAsync(string key, CancellationToken ct); Task<IReadOnlyList<License>> GetByStatusAsync(LicenseStatus s, CancellationToken ct); }
// P100
public interface ISupportTicketRepository : IRepository<SupportTicket> { Task<IReadOnlyList<SupportTicket>> GetByCustomerAsync(Guid cId, CancellationToken ct); }
// P101
public interface IUpdateDeploymentRepository : IRepository<UpdateDeployment> { Task<IReadOnlyList<UpdateDeployment>> GetRecentAsync(int limit, CancellationToken ct); }
// P102
public interface IOfficialModuleRepository : IRepository<OfficialModule> { Task<IReadOnlyList<OfficialModule>> GetPublishedAsync(CancellationToken ct); }
// P103
public interface IFeeDefaulterRepository : IRepository<FeeDefaulterRecord> { Task<IReadOnlyList<FeeDefaulterRecord>> GetByStudentAsync(Guid sId, CancellationToken ct); Task<IReadOnlyList<FeeDefaulterRecord>> GetPendingAsync(CancellationToken ct); }
// P104
public interface IMisconductCaseRepository : IRepository<MisconductCase> { Task<IReadOnlyList<MisconductCase>> GetByStudentAsync(Guid sId, CancellationToken ct); Task<IReadOnlyList<MisconductCase>> GetByStatusAsync(MisconductStatus s, CancellationToken ct); }
// P105
public interface IFacultyWorkloadRepository : IRepository<FacultyWorkload> { Task<IReadOnlyList<FacultyWorkload>> GetByFacultyAsync(Guid fId, CancellationToken ct); }
// P106
public interface IDegreeVerificationRepository : IRepository<DegreeVerification> { Task<DegreeVerification?> GetByTokenAsync(string token, CancellationToken ct); }
// P107
public interface IRegistrationWindowRepository : IRepository<RegistrationWindow> { Task<RegistrationWindow?> GetActiveAsync(CancellationToken ct); }
public interface IRegistrationEnrollmentRepository : IRepository<RegistrationEnrollment> { Task<IReadOnlyList<RegistrationEnrollment>> GetByStudentAsync(Guid sId, CancellationToken ct); Task<IReadOnlyList<RegistrationEnrollment>> GetByWindowAsync(Guid wId, CancellationToken ct); }
// P108
public interface ICurriculumProposalRepository : IRepository<CurriculumProposal> { Task<IReadOnlyList<CurriculumProposal>> GetByStatusAsync(CurriculumApprovalStatus s, CancellationToken ct); }
// P109
public interface IAccessAuditLogRepository : IRepository<AccessAuditLog> { Task<IReadOnlyList<AccessAuditLog>> GetBlockedAsync(CancellationToken ct); }
// P110
public interface IHardcodingFindingRepository : IRepository<HardcodingFinding> { Task<IReadOnlyList<HardcodingFinding>> GetBySeverityAsync(HardcodingSeverity s, CancellationToken ct); Task<IReadOnlyList<HardcodingFinding>> GetUnresolvedAsync(CancellationToken ct); }
