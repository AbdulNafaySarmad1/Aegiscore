using AegisCore.Domain.Entities;

namespace AegisCore.Application.Platform;

// P87 Queue Management
public interface IQueueManagementService { Task<QueueDashboardDto> GetDashboardAsync(CancellationToken ct); Task RetryJobAsync(Guid jobId, CancellationToken ct); Task MoveToDeadLetterAsync(Guid jobId, CancellationToken ct); }
// P88 Event Explorer
public interface IEventExplorerService { Task<List<DomainEvent>> GetEventsAsync(string? type, string? aggregateId, DateTime? from, DateTime? to, CancellationToken ct); Task ReplayEventAsync(Guid eventId, CancellationToken ct); Task<EventSchemaDto> GetSchemaAsync(string eventType, CancellationToken ct); }
// P89 Diagnostics
public interface IDiagnosticsService { Task<DiagnosticReportDto> RunDiagnosticScanAsync(CancellationToken ct); Task<List<DiagnosticFinding>> GetFindingsAsync(CancellationToken ct); }
// P90 Architecture Explorer
public interface IArchitectureExplorerService { Task<ArchitectureMapDto> GetArchitectureMapAsync(CancellationToken ct); Task RegisterServiceAsync(ServiceRegistration service, CancellationToken ct); Task RegisterEndpointAsync(ApiEndpoint endpoint, CancellationToken ct); }
// P91 Health Analyzer
public interface IHealthAnalyzerService { Task<HealthScore> GenerateReportAsync(CancellationToken ct); Task<HealthScore?> GetLatestReportAsync(CancellationToken ct); }
// P92 Integration Monitoring
public interface IIntegrationMonitoringService { Task<IntegrationStatus> CheckIntegrationAsync(Guid integrationId, CancellationToken ct); Task CheckAllIntegrationsAsync(CancellationToken ct); Task<List<IntegrationRegistration>> GetStatusAsync(CancellationToken ct); }
// P93 Benchmark
public interface IBenchmarkService { Task<BenchmarkRun> StartBenchmarkAsync(string scenario, BenchmarkTier tier, CancellationToken ct); Task<BenchmarkRun?> GetLatestRunAsync(CancellationToken ct); }
// P94 AI Operations
public interface IAIOperationsService { Task<AIProvider> RegisterProviderAsync(AIProvider provider, CancellationToken ct); Task<AIModel> RegisterModelAsync(AIModel model, CancellationToken ct); Task<AITokenUsageSummaryDto> GetTokenUsageAsync(string? domain, DateTime? from, DateTime? to, CancellationToken ct); }
// P95 Prompt Management
public interface IPromptManagementService { Task<Prompt> CreatePromptAsync(Prompt prompt, CancellationToken ct); Task<Prompt> ActivateVersionAsync(Guid promptId, int version, CancellationToken ct); Task<PromptTestResultDto> TestPromptAsync(Guid promptId, int version, string input, CancellationToken ct); Task<PromptABTest> StartABTestAsync(Guid promptId, int vA, int vB, double split, CancellationToken ct); }
// P96 AI Guardrails
public interface IGuardrailService { Task<GuardrailResultDto> EvaluateAsync(string domain, string input, string? output, CancellationToken ct); Task<List<GuardrailViolation>> GetViolationsAsync(int limit, CancellationToken ct); }
// P97 AI Analytics
public interface IAIAnalyticsService { Task RecordFeedbackAsync(Guid userId, string domain, bool thumbsUp, int rating, string? comment, CancellationToken ct); Task<AIAnalyticsDto> GetAnalyticsAsync(string domain, DateTime? from, DateTime? to, CancellationToken ct); }
// P98 License
public interface ILicenseService { Task<License> IssueLicenseAsync(string customerName, string email, LicenseEdition edition, int maxUsers, DateTime? expiresAt, CancellationToken ct); Task<bool> ValidateLicenseAsync(string licenseKey, CancellationToken ct); Task RevokeLicenseAsync(Guid licenseId, string reason, CancellationToken ct); }
// P100 Customer Portal
public interface ICustomerPortalService { Task<SupportTicket> CreateTicketAsync(string subject, string description, Guid customerId, CancellationToken ct); Task<List<ReleaseDownload>> GetReleasesAsync(CancellationToken ct); }
// P101 Update
public interface IUpdateService { Task<UpdateDeployment> StartUpdateAsync(string fromVersion, string toVersion, CancellationToken ct); Task RollbackAsync(Guid deploymentId, CancellationToken ct); Task<List<UpdateDeployment>> GetHistoryAsync(CancellationToken ct); }
// P102 Official Marketplace
public interface IOfficialMarketplaceService { Task<OfficialModule> PublishModuleAsync(OfficialModule module, CancellationToken ct); Task<List<OfficialModule>> GetCatalogAsync(CancellationToken ct); }
// P103 Fee Defaulter
public interface IFeeDefaulterService { Task EscalateAsync(Guid recordId, CancellationToken ct); Task MarkAsClearedAsync(Guid recordId, CancellationToken ct); Task<List<FeeDefaulterRecord>> GetPendingAsync(CancellationToken ct); }
// P104 Misconduct
public interface IMisconductService { Task<MisconductCase> ReportAsync(MisconductCase c, CancellationToken ct); Task AssignInvestigatorAsync(Guid caseId, Guid userId, CancellationToken ct); Task ResolveAsync(Guid caseId, SanctionType sanction, string resolution, CancellationToken ct); }
// P105 Faculty Workload
public interface IFacultyWorkloadService { Task<FacultyWorkload> AssignAsync(FacultyWorkload wl, CancellationToken ct); Task<List<FacultyWorkload>> GetWorkloadAsync(Guid facultyId, CancellationToken ct); }
// P106 Degree Verification
public interface IDegreeVerificationService { Task<string> GenerateVerificationTokenAsync(Guid studentId, CancellationToken ct); Task<VerificationResultDto> VerifyAsync(string token, CancellationToken ct); }
// P107 Registration
public interface IRegistrationService { Task<RegistrationWindow> OpenWindowAsync(RegistrationWindow w, CancellationToken ct); Task<RegistrationEnrollment> EnrollAsync(Guid windowId, Guid studentId, Guid courseId, CancellationToken ct); Task<bool> IsOpenForRegistrationAsync(CancellationToken ct); }
// P108 Curriculum Governance
public interface ICurriculumGovernanceService { Task<CurriculumProposal> ProposeAsync(CurriculumProposal p, CancellationToken ct); Task ApproveAsync(Guid proposalId, Guid userId, CancellationToken ct); Task RejectAsync(Guid proposalId, Guid userId, string reason, CancellationToken ct); }
// P109 Zero Trust
public interface IZeroTrustService { Task<TunnelConfiguration> ConfigureTunnelAsync(string name, string tunnelUrl, CancellationToken ct); Task<bool> IsTunnelActiveAsync(CancellationToken ct); }
// P110 Hardcoding Audit
public interface IHardcodingAuditService { Task<HardcodingFinding> ReportAsync(HardcodingFinding f, CancellationToken ct); Task<List<HardcodingFinding>> GetUnresolvedAsync(CancellationToken ct); Task MarkResolvedAsync(Guid findingId, CancellationToken ct); }

// Consolidated DTOs
public class QueueDashboardDto { public int TotalQueues { get; set; } public int HealthyQueues { get; set; } public int BackloggedQueues { get; set; } public int TotalPending { get; set; } public int TotalFailed { get; set; } public int DeadLetterCount { get; set; } }
public class EventSchemaDto { public string EventType { get; set; } = ""; public int CurrentVersion { get; set; } public object? Schema { get; set; } }
public class DiagnosticReportDto { public int TotalFindings { get; set; } public int SlowQueries { get; set; } public double ApiP95Ms { get; set; } public double CacheHitRate { get; set; } public DateTime GeneratedAt { get; set; } }
public class ArchitectureMapDto { public List<ServiceRegistration> Services { get; set; } = new(); public List<ServiceDependency> Dependencies { get; set; } = new(); public List<ApiEndpoint> Endpoints { get; set; } = new(); }
public class AITokenUsageSummaryDto { public long TotalInputTokens { get; set; } public long TotalOutputTokens { get; set; } public decimal EstimatedCost { get; set; } public long TotalRequests { get; set; } public long FailedRequests { get; set; } public double AvgLatencyMs { get; set; } }
public class PromptTestResultDto { public string Response { get; set; } = ""; public int TokensUsed { get; set; } public int LatencyMs { get; set; } }
public class GuardrailResultDto { public bool IsAllowed { get; set; } public string? BlockReason { get; set; } public GuardrailAction Action { get; set; } }
public class AIAnalyticsDto { public long TotalRequests { get; set; } public long ThumbsUp { get; set; } public long ThumbsDown { get; set; } public double AvgRating { get; set; } public decimal TotalCost { get; set; } public double AvgLatencyMs { get; set; } }
public class VerificationResultDto { public bool IsValid { get; set; } public string? StudentName { get; set; } public string? Degree { get; set; } public string? GraduationYear { get; set; } public string Status { get; set; } = ""; }
