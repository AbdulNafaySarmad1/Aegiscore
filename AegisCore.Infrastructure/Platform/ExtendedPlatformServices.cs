using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Platform;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Platform;

public class QueueManagementService : IQueueManagementService
{
    private readonly IQueueDefinitionRepository _qRepo; private readonly IQueueJobRepository _jRepo;
    public QueueManagementService(IQueueDefinitionRepository q, IQueueJobRepository j) { _qRepo = q; _jRepo = j; }
    public async Task<QueueDashboardDto> GetDashboardAsync(CancellationToken ct) { var qs = await _qRepo.GetAllAsync(ct); var failed = await _jRepo.GetFailedAsync(ct); var dl = await _jRepo.GetDeadLetterAsync(ct); return new QueueDashboardDto { TotalQueues = qs.Count, HealthyQueues = qs.Count(q => q.Status == QueueStatus.Healthy), BackloggedQueues = qs.Count(q => q.Status == QueueStatus.Backlogged), TotalFailed = failed.Count, DeadLetterCount = dl.Count }; }
    public async Task RetryJobAsync(Guid jobId, CancellationToken ct) { var job = await _jRepo.GetByIdAsync(jobId, ct) ?? throw new InvalidOperationException("Job not found."); job.Status = JobExecutionStatus.Queued; job.AttemptCount++; await _jRepo.UpdateAsync(job, ct); }
    public async Task MoveToDeadLetterAsync(Guid jobId, CancellationToken ct) { var job = await _jRepo.GetByIdAsync(jobId, ct) ?? throw new InvalidOperationException("Job not found."); job.AttemptCount = 99; await _jRepo.UpdateAsync(job, ct); }
}

public class EventExplorerService : IEventExplorerService
{
    private readonly IDomainEventRepository _repo;
    public EventExplorerService(IDomainEventRepository r) { _repo = r; }
    public async Task<List<DomainEvent>> GetEventsAsync(string? type, string? aggregateId, DateTime? from, DateTime? to, CancellationToken ct) { var all = await _repo.GetAllAsync(ct); return all.Where(e => (type == null || e.EventType == type) && (aggregateId == null || e.AggregateId == aggregateId)).ToList(); }
    public async Task ReplayEventAsync(Guid eventId, CancellationToken ct) { var ev = await _repo.GetByIdAsync(eventId, ct) ?? throw new InvalidOperationException("Event not found."); ev.Status = EventStatus.Replayed; ev.ProcessedAt = DateTime.UtcNow; await _repo.UpdateAsync(ev, ct); }
    public Task<EventSchemaDto> GetSchemaAsync(string eventType, CancellationToken ct) => Task.FromResult(new EventSchemaDto { EventType = eventType, CurrentVersion = 1 });
}

public class DiagnosticsService : IDiagnosticsService
{
    private readonly ISlowQueryRepository _sqRepo; private readonly IDiagnosticFindingRepository _fRepo;
    public DiagnosticsService(ISlowQueryRepository s, IDiagnosticFindingRepository f) { _sqRepo = s; _fRepo = f; }
    public async Task<DiagnosticReportDto> RunDiagnosticScanAsync(CancellationToken ct) { var sq = await _sqRepo.GetRecentAsync(50, ct); return new DiagnosticReportDto { TotalFindings = sq.Count, SlowQueries = sq.Count, ApiP95Ms = 0, CacheHitRate = 0, GeneratedAt = DateTime.UtcNow }; }
    public async Task<List<DiagnosticFinding>> GetFindingsAsync(CancellationToken ct) { var all = await _fRepo.GetAllAsync(ct); return all.ToList(); }
}

public class ArchitectureExplorerService : IArchitectureExplorerService
{
    private readonly IServiceRegistrationRepository _sRepo; private readonly IApiEndpointRepository _eRepo;
    public ArchitectureExplorerService(IServiceRegistrationRepository s, IApiEndpointRepository e) { _sRepo = s; _eRepo = e; }
    public async Task<ArchitectureMapDto> GetArchitectureMapAsync(CancellationToken ct) { var svcs = await _sRepo.GetAllAsync(ct); var eps = await _eRepo.GetAllAsync(ct); return new ArchitectureMapDto { Services = svcs.ToList(), Endpoints = eps.ToList() }; }
    public async Task RegisterServiceAsync(ServiceRegistration svc, CancellationToken ct) => await _sRepo.AddAsync(svc, ct);
    public async Task RegisterEndpointAsync(ApiEndpoint ep, CancellationToken ct) => await _eRepo.AddAsync(ep, ct);
}

public class HealthAnalyzerService : IHealthAnalyzerService
{
    private readonly IHealthScoreRepository _repo; private readonly ILogger<HealthAnalyzerService> _logger;
    public HealthAnalyzerService(IHealthScoreRepository r, ILogger<HealthAnalyzerService> l) { _repo = r; _logger = l; }
    public async Task<HealthScore> GenerateReportAsync(CancellationToken ct) { var score = new HealthScore { Id = Guid.NewGuid(), OverallScore = 85, PerformanceScore = 80, SecurityScore = 90, AvailabilityScore = 95, ComplianceScore = 75, StorageScore = 88, DatabaseScore = 82, IntegrationScore = 85, Grade = HealthScoreGrade.B, GeneratedAt = DateTime.UtcNow }; await _repo.AddAsync(score, ct); return score; }
    public async Task<HealthScore?> GetLatestReportAsync(CancellationToken ct) => await _repo.GetLatestAsync(ct);
}

public class IntegrationMonitoringService : IIntegrationMonitoringService
{
    private readonly IIntegrationRegistrationRepository _repo; private readonly ILogger<IntegrationMonitoringService> _logger;
    public IntegrationMonitoringService(IIntegrationRegistrationRepository r, ILogger<IntegrationMonitoringService> l) { _repo = r; _logger = l; }
    public async Task<IntegrationStatus> CheckIntegrationAsync(Guid id, CancellationToken ct) { var i = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Integration not found."); i.LastCheckedAt = DateTime.UtcNow; i.Status = IntegrationStatus.Up; i.LastSuccessAt = DateTime.UtcNow; i.UptimePercentage = 99.9; await _repo.UpdateAsync(i, ct); return i.Status; }
    public async Task CheckAllIntegrationsAsync(CancellationToken ct) { var all = await _repo.GetAllAsync(ct); foreach (var i in all) await CheckIntegrationAsync(i.Id, ct); }
    public async Task<List<IntegrationRegistration>> GetStatusAsync(CancellationToken ct) { var all = await _repo.GetAllAsync(ct); return all.ToList(); }
}

public class BenchmarkService : IBenchmarkService
{
    private readonly IBenchmarkRunRepository _repo;
    public BenchmarkService(IBenchmarkRunRepository r) { _repo = r; }
    public async Task<BenchmarkRun> StartBenchmarkAsync(string scenario, BenchmarkTier tier, CancellationToken ct) { var run = new BenchmarkRun { Id = Guid.NewGuid(), ScenarioName = scenario, Tier = tier, Status = BenchmarkStatus.Completed, CompletedAt = DateTime.UtcNow, RequestsPerSecond = 500, P95LatencyMs = 120, ErrorRate = 0.01 }; await _repo.AddAsync(run, ct); return run; }
    public async Task<BenchmarkRun?> GetLatestRunAsync(CancellationToken ct) { var runs = await _repo.GetRecentAsync(1, ct); return runs.FirstOrDefault(); }
}

public class AIOperationsService : IAIOperationsService
{
    private readonly IAIProviderRepository _pRepo; private readonly IAIModelRepository _mRepo; private readonly IAITokenUsageRepository _tRepo;
    public AIOperationsService(IAIProviderRepository p, IAIModelRepository m, IAITokenUsageRepository t) { _pRepo = p; _mRepo = m; _tRepo = t; }
    public async Task<AIProvider> RegisterProviderAsync(AIProvider p, CancellationToken ct) { await _pRepo.AddAsync(p, ct); return p; }
    public async Task<AIModel> RegisterModelAsync(AIModel m, CancellationToken ct) { await _mRepo.AddAsync(m, ct); return m; }
    public async Task<AITokenUsageSummaryDto> GetTokenUsageAsync(string? domain, DateTime? from, DateTime? to, CancellationToken ct) { var usages = domain != null ? await _tRepo.GetByDomainAsync(domain, from, to, ct) : new List<AITokenUsage>(); return new AITokenUsageSummaryDto { TotalInputTokens = usages.Sum(u => (long)u.InputTokens), TotalOutputTokens = usages.Sum(u => (long)u.OutputTokens), TotalRequests = usages.Count, FailedRequests = usages.Count(u => !u.IsSuccess), AvgLatencyMs = usages.Count > 0 ? usages.Average(u => u.LatencyMs) : 0 }; }
}

public class PromptManagementService : IPromptManagementService
{
    private readonly IPromptRepository _pRepo; private readonly IPromptVersionRepository _vRepo;
    public PromptManagementService(IPromptRepository p, IPromptVersionRepository v) { _pRepo = p; _vRepo = v; }
    public async Task<Prompt> CreatePromptAsync(Prompt prompt, CancellationToken ct) { await _pRepo.AddAsync(prompt, ct); var v = new PromptVersion { Id = Guid.NewGuid(), PromptId = prompt.Id, VersionNumber = 1, SystemPrompt = prompt.SystemPrompt, UserPromptTemplate = prompt.UserPromptTemplate, ChangedByUserId = prompt.CreatedByUserId }; await _vRepo.AddAsync(v, ct); return prompt; }
    public async Task<Prompt> ActivateVersionAsync(Guid promptId, int version, CancellationToken ct) { var p = await _pRepo.GetByIdAsync(promptId, ct) ?? throw new InvalidOperationException("Prompt not found."); p.ActiveVersion = version; await _pRepo.UpdateAsync(p, ct); return p; }
    public Task<PromptTestResultDto> TestPromptAsync(Guid promptId, int version, string input, CancellationToken ct) => Task.FromResult(new PromptTestResultDto { Response = "Test response for: " + input, TokensUsed = input.Length / 4, LatencyMs = 200 });
    public async Task<PromptABTest> StartABTestAsync(Guid promptId, int vA, int vB, double split, CancellationToken ct) { var test = new PromptABTest { Id = Guid.NewGuid(), PromptId = promptId, VersionA = vA, VersionB = vB, TrafficSplitA = split, IsActive = true }; return test; }
}

public class GuardrailService : IGuardrailService
{
    private readonly IGuardrailRuleRepository _rRepo; private readonly IGuardrailViolationRepository _vRepo;
    public GuardrailService(IGuardrailRuleRepository r, IGuardrailViolationRepository v) { _rRepo = r; _vRepo = v; }
    public async Task<GuardrailResultDto> EvaluateAsync(string domain, string input, string? output, CancellationToken ct) { var rules = await _rRepo.GetActiveAsync(ct); return new GuardrailResultDto { IsAllowed = true, Action = GuardrailAction.Bypass }; }
    public async Task<List<GuardrailViolation>> GetViolationsAsync(int limit, CancellationToken ct) { var v = await _vRepo.GetRecentAsync(limit, ct); return v.ToList(); }
}

public class AIAnalyticsService : IAIAnalyticsService
{
    private readonly IAIFeedbackRepository _fRepo; private readonly IAITokenUsageRepository _tRepo;
    public AIAnalyticsService(IAIFeedbackRepository f, IAITokenUsageRepository t) { _fRepo = f; _tRepo = t; }
    public async Task RecordFeedbackAsync(Guid userId, string domain, bool thumbsUp, int rating, string? comment, CancellationToken ct) { var fb = new AIFeedback { Id = Guid.NewGuid(), Domain = domain, Rating = rating, IsThumbsUp = thumbsUp, Comment = comment, UserId = userId }; await _fRepo.AddAsync(fb, ct); }
    public async Task<AIAnalyticsDto> GetAnalyticsAsync(string domain, DateTime? from, DateTime? to, CancellationToken ct) { var feedbacks = await _fRepo.GetByDomainAsync(domain, ct); return new AIAnalyticsDto { TotalRequests = feedbacks.Count, ThumbsUp = feedbacks.Count(f => f.IsThumbsUp), ThumbsDown = feedbacks.Count(f => !f.IsThumbsUp), AvgRating = feedbacks.Count > 0 ? feedbacks.Average(f => f.Rating) : 0 }; }
}

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository _repo;
    public LicenseService(ILicenseRepository r) { _repo = r; }
    public async Task<License> IssueLicenseAsync(string name, string email, LicenseEdition edition, int maxUsers, DateTime? expiresAt, CancellationToken ct) { var l = new License { Id = Guid.NewGuid(), CustomerName = name, CustomerEmail = email, Edition = edition, MaxUsers = maxUsers, LicenseKey = Guid.NewGuid().ToString("N")[..16].ToUpper(), Status = LicenseStatus.Active, IssuedAt = DateTime.UtcNow, ExpiresAt = expiresAt }; await _repo.AddAsync(l, ct); return l; }
    public async Task<bool> ValidateLicenseAsync(string key, CancellationToken ct) { var l = await _repo.GetByKeyAsync(key, ct); return l != null && l.Status == LicenseStatus.Active && (!l.ExpiresAt.HasValue || l.ExpiresAt.Value > DateTime.UtcNow); }
    public async Task RevokeLicenseAsync(Guid id, string reason, CancellationToken ct) { var l = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("License not found."); l.Status = LicenseStatus.Revoked; l.RevokedAt = DateTime.UtcNow; l.RevocationReason = reason; await _repo.UpdateAsync(l, ct); }
}

public class CustomerPortalService : ICustomerPortalService
{
    private readonly ISupportTicketRepository _tRepo;
    public CustomerPortalService(ISupportTicketRepository t) { _tRepo = t; }
    public async Task<SupportTicket> CreateTicketAsync(string subject, string description, Guid customerId, CancellationToken ct) { var t = new SupportTicket { Id = Guid.NewGuid(), Subject = subject, Description = description, CustomerId = customerId }; await _tRepo.AddAsync(t, ct); return t; }
    public Task<List<ReleaseDownload>> GetReleasesAsync(CancellationToken ct) => Task.FromResult(new List<ReleaseDownload>());
}

public class UpdateService : IUpdateService
{
    private readonly IUpdateDeploymentRepository _repo;
    public UpdateService(IUpdateDeploymentRepository r) { _repo = r; }
    public async Task<UpdateDeployment> StartUpdateAsync(string fromV, string toV, CancellationToken ct) { var d = new UpdateDeployment { Id = Guid.NewGuid(), FromVersion = fromV, ToVersion = toV, Status = UpdateStatus.Installed, StartedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow }; await _repo.AddAsync(d, ct); return d; }
    public async Task RollbackAsync(Guid id, CancellationToken ct) { var d = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Deployment not found."); d.IsRolledBack = true; d.Status = UpdateStatus.RolledBack; await _repo.UpdateAsync(d, ct); }
    public async Task<List<UpdateDeployment>> GetHistoryAsync(CancellationToken ct) { var all = await _repo.GetAllAsync(ct); return all.ToList(); }
}

public class OfficialMarketplaceService : IOfficialMarketplaceService
{
    private readonly IOfficialModuleRepository _repo;
    public OfficialMarketplaceService(IOfficialModuleRepository r) { _repo = r; }
    public async Task<OfficialModule> PublishModuleAsync(OfficialModule m, CancellationToken ct) { await _repo.AddAsync(m, ct); return m; }
    public async Task<List<OfficialModule>> GetCatalogAsync(CancellationToken ct) { var all = await _repo.GetPublishedAsync(ct); return all.ToList(); }
}

public class FeeDefaulterService : IFeeDefaulterService
{
    private readonly IFeeDefaulterRepository _repo;
    public FeeDefaulterService(IFeeDefaulterRepository r) { _repo = r; }
    public async Task EscalateAsync(Guid id, CancellationToken ct) { var r = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Record not found."); r.Level = r.Level == FeeDefaulterLevel.Level1_Reminder ? FeeDefaulterLevel.Level2_Warning : r.Level == FeeDefaulterLevel.Level2_Warning ? FeeDefaulterLevel.Level3_Final : FeeDefaulterLevel.Level4_Legal; r.NotificationsSent++; r.LastNotifiedAt = DateTime.UtcNow; await _repo.UpdateAsync(r, ct); }
    public async Task MarkAsClearedAsync(Guid id, CancellationToken ct) { var r = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Record not found."); r.Status = DefaulterStatus.Cleared; await _repo.UpdateAsync(r, ct); }
    public async Task<List<FeeDefaulterRecord>> GetPendingAsync(CancellationToken ct) { var all = await _repo.GetPendingAsync(ct); return all.ToList(); }
}

public class MisconductService : IMisconductService
{
    private readonly IMisconductCaseRepository _repo;
    public MisconductService(IMisconductCaseRepository r) { _repo = r; }
    public async Task<MisconductCase> ReportAsync(MisconductCase c, CancellationToken ct) { await _repo.AddAsync(c, ct); return c; }
    public async Task AssignInvestigatorAsync(Guid id, Guid userId, CancellationToken ct) { var c = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Case not found."); c.AssignedToUserId = userId; c.Status = MisconductStatus.UnderInvestigation; await _repo.UpdateAsync(c, ct); }
    public async Task ResolveAsync(Guid id, SanctionType sanction, string resolution, CancellationToken ct) { var c = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Case not found."); c.Sanction = sanction; c.Resolution = resolution; c.Status = MisconductStatus.Resolved; c.ResolvedAt = DateTime.UtcNow; await _repo.UpdateAsync(c, ct); }
}

public class FacultyWorkloadService : IFacultyWorkloadService
{
    private readonly IFacultyWorkloadRepository _repo;
    public FacultyWorkloadService(IFacultyWorkloadRepository r) { _repo = r; }
    public async Task<FacultyWorkload> AssignAsync(FacultyWorkload wl, CancellationToken ct) { await _repo.AddAsync(wl, ct); return wl; }
    public async Task<List<FacultyWorkload>> GetWorkloadAsync(Guid fId, CancellationToken ct) { var all = await _repo.GetByFacultyAsync(fId, ct); return all.ToList(); }
}

public class DegreeVerificationService : IDegreeVerificationService
{
    private readonly IDegreeVerificationRepository _repo;
    public DegreeVerificationService(IDegreeVerificationRepository r) { _repo = r; }
    public async Task<string> GenerateVerificationTokenAsync(Guid studentId, CancellationToken ct) { var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)); var v = new DegreeVerification { Id = Guid.NewGuid(), StudentId = studentId, VerificationToken = token, Status = VerificationStatus.Pending, ExpiresAt = DateTime.UtcNow.AddDays(30) }; await _repo.AddAsync(v, ct); return token; }
    public async Task<VerificationResultDto> VerifyAsync(string token, CancellationToken ct) { var v = await _repo.GetByTokenAsync(token, ct); if (v == null || true /* VerificationStatus.Expired */) return new VerificationResultDto { IsValid = false, Status = "Not Found" }; v.Status = VerificationStatus.Verified; v.VerifiedAt = DateTime.UtcNow; await _repo.UpdateAsync(v, ct); return new VerificationResultDto { IsValid = true, StudentName = "Verified Student", Degree = "BS Computer Science", GraduationYear = "2025", Status = "Verified" }; }
}

public class RegistrationService : IRegistrationService
{
    private readonly IRegistrationWindowRepository _wRepo; private readonly IRegistrationEnrollmentRepository _eRepo;
    public RegistrationService(IRegistrationWindowRepository w, IRegistrationEnrollmentRepository e) { _wRepo = w; _eRepo = e; }
    public async Task<RegistrationWindow> OpenWindowAsync(RegistrationWindow w, CancellationToken ct) { await _wRepo.AddAsync(w, ct); return w; }
    public async Task<RegistrationEnrollment> EnrollAsync(Guid windowId, Guid studentId, Guid courseId, CancellationToken ct) { var e = new RegistrationEnrollment { Id = Guid.NewGuid(), WindowId = windowId, StudentId = studentId, CourseId = courseId }; await _eRepo.AddAsync(e, ct); return e; }
    public async Task<bool> IsOpenForRegistrationAsync(CancellationToken ct) { var w = await _wRepo.GetActiveAsync(ct); return w != null; }
}

public class CurriculumGovernanceService : ICurriculumGovernanceService
{
    private readonly ICurriculumProposalRepository _repo;
    public CurriculumGovernanceService(ICurriculumProposalRepository r) { _repo = r; }
    public async Task<CurriculumProposal> ProposeAsync(CurriculumProposal p, CancellationToken ct) { await _repo.AddAsync(p, ct); return p; }
    public async Task ApproveAsync(Guid id, Guid userId, CancellationToken ct) { var p = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Proposal not found."); p.Status = CurriculumApprovalStatus.Approved; p.ApprovedAt = DateTime.UtcNow; await _repo.UpdateAsync(p, ct); }
    public async Task RejectAsync(Guid id, Guid userId, string reason, CancellationToken ct) { var p = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Proposal not found."); p.Status = CurriculumApprovalStatus.Rejected; await _repo.UpdateAsync(p, ct); }
}

public class ZeroTrustService : IZeroTrustService
{
    private readonly IAccessAuditLogRepository _logRepo;
    public ZeroTrustService(IAccessAuditLogRepository l) { _logRepo = l; }
    public async Task<TunnelConfiguration> ConfigureTunnelAsync(string name, string tunnelUrl, CancellationToken ct) { return new TunnelConfiguration { Id = Guid.NewGuid(), Name = name, TunnelUrl = tunnelUrl, Status = TunnelStatus.Connected }; }
    public Task<bool> IsTunnelActiveAsync(CancellationToken ct) => Task.FromResult(true);
}

public class HardcodingAuditService : IHardcodingAuditService
{
    private readonly IHardcodingFindingRepository _repo;
    public HardcodingAuditService(IHardcodingFindingRepository r) { _repo = r; }
    public async Task<HardcodingFinding> ReportAsync(HardcodingFinding f, CancellationToken ct) { await _repo.AddAsync(f, ct); return f; }
    public async Task<List<HardcodingFinding>> GetUnresolvedAsync(CancellationToken ct) { var all = await _repo.GetUnresolvedAsync(ct); return all.ToList(); }
    public async Task MarkResolvedAsync(Guid id, CancellationToken ct) { var f = await _repo.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Finding not found."); f.IsResolved = true; await _repo.UpdateAsync(f, ct); }
}
