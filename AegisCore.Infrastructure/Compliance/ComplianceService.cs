using AegisCore.Application.Compliance;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace AegisCore.Infrastructure.Compliance;

public class ComplianceService : IComplianceService
{
    private readonly IComplianceFrameworkRepository _frameworkRepo;
    private readonly IComplianceControlRepository _controlRepo;
    private readonly IAuditFindingRepository _findingRepo;
    private readonly ICorrectiveActionPlanRepository _capRepo;
    private readonly ILogger<ComplianceService> _logger;
    private readonly AegisDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ComplianceService(
        IComplianceFrameworkRepository frameworkRepo,
        IComplianceControlRepository controlRepo,
        IAuditFindingRepository findingRepo,
        ICorrectiveActionPlanRepository capRepo,
        AegisDbContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ComplianceService> logger)
    {
        _frameworkRepo = frameworkRepo;
        _controlRepo = controlRepo;
        _findingRepo = findingRepo;
        _capRepo = capRepo;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ComplianceFramework> RegisterFrameworkAsync(string name, string code, string description, CancellationToken cancellationToken = default)
    {
        var existing = await _frameworkRepo.GetByCodeAsync(code, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Framework '{code}' already exists.");

        var framework = new ComplianceFramework
        {
            Id = Guid.NewGuid(), TenantId = TenantId(), Name = name, Code = code, Description = description, Status = ComplianceFrameworkStatus.Active
        };

        await _frameworkRepo.AddAsync(framework, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Compliance framework registered: {Name} ({Code})", name, code);
        return framework;
    }

    public async Task<ComplianceControl> AddControlAsync(Guid frameworkId, string controlCode, string title, string description, Guid ownerUserId, string ownerName, CancellationToken cancellationToken = default)
    {
        var framework = await _frameworkRepo.GetByIdAsync(frameworkId, cancellationToken)
            ?? throw new InvalidOperationException("Framework not found.");

        var control = new ComplianceControl
        {
            Id = Guid.NewGuid(), TenantId = TenantId(), FrameworkId = frameworkId, ControlCode = controlCode, Title = title,
            Description = description, OwnerUserId = ownerUserId, OwnerName = ownerName,
            Status = ControlStatus.InProgress, NextReviewDate = DateTime.UtcNow.AddYears(1)
        };

        await _controlRepo.AddAsync(control, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return control;
    }

    public async Task<ComplianceControl> UpdateControlStatusAsync(Guid controlId, ControlStatus newStatus, string? notes, CancellationToken cancellationToken = default)
    {
        var control = await _controlRepo.GetByIdAsync(controlId, cancellationToken)
            ?? throw new InvalidOperationException("Control not found.");

        control.Status = newStatus;
        control.LastReviewedAt = DateTime.UtcNow;
        await _controlRepo.UpdateAsync(control, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Control {Code} status updated to {Status}", control.ControlCode, newStatus);
        return control;
    }

    public async Task<ControlEvidence> AttachEvidenceAsync(Guid controlId, string fileUrl, string description, Guid uploadedByUserId, CancellationToken cancellationToken = default)
    {
        _ = await _controlRepo.GetByIdAsync(controlId, cancellationToken) ?? throw new InvalidOperationException("Control not found.");
        var evidence = new ControlEvidence
        {
            Id = Guid.NewGuid(), TenantId = TenantId(), ControlId = controlId, FileUrl = fileUrl,
            Description = description, UploadedByUserId = uploadedByUserId
        };

        _db.ControlEvidences.Add(evidence);
        await _db.SaveChangesAsync(cancellationToken);
        return evidence;
    }

    public async Task<AuditFinding> ReportFindingAsync(Guid controlId, string title, string description, AuditFindingSeverity severity, Guid identifiedById, string identifiedByName, CancellationToken cancellationToken = default)
    {
        var finding = new AuditFinding
        {
            Id = Guid.NewGuid(), TenantId = TenantId(), ControlId = controlId, Title = title, Description = description,
            Severity = severity, Status = AuditFindingStatus.Open,
            IdentifiedByUserId = identifiedById, IdentifiedByName = identifiedByName
        };

        await _findingRepo.AddAsync(finding, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Audit finding reported: {Title} ({Severity})", title, severity);
        return finding;
    }

    public async Task<CorrectiveActionPlan> CreateActionPlanAsync(Guid findingId, string description, Guid assigneeId, string assigneeName, DateTime dueDate, CancellationToken cancellationToken = default)
    {
        var cap = new CorrectiveActionPlan
        {
            Id = Guid.NewGuid(), TenantId = TenantId(), FindingId = findingId, Description = description,
            AssignedToUserId = assigneeId, AssignedToName = assigneeName, DueDate = dueDate,
            Status = CAPStatus.Planned
        };

        await _capRepo.AddAsync(cap, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return cap;
    }

    public async Task<CorrectiveActionPlan> CompleteActionPlanAsync(Guid capId, string completionNotes, CancellationToken cancellationToken = default)
    {
        var cap = await _capRepo.GetByIdAsync(capId, cancellationToken)
            ?? throw new InvalidOperationException("CAP not found.");

        cap.Status = CAPStatus.Completed;
        cap.CompletedAt = DateTime.UtcNow;
        cap.CompletionNotes = completionNotes;
        await _capRepo.UpdateAsync(cap, cancellationToken);

        var finding = await _findingRepo.GetWithActionPlansAsync(cap.FindingId, cancellationToken);
        if (finding != null && finding.ActionPlans.All(a => a.Status == CAPStatus.Completed))
        {
            finding.Status = AuditFindingStatus.Resolved;
            finding.ResolvedAt = DateTime.UtcNow;
            await _findingRepo.UpdateAsync(finding, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return cap;
    }

    public async Task<ComplianceDashboardDto> GetDashboardAsync(Guid? frameworkId, CancellationToken cancellationToken = default)
    {
        var frameworks = await _frameworkRepo.GetAllAsync(cancellationToken);
        var allControls = new List<ComplianceControl>();
        foreach (var fw in frameworks)
        {
            var fwControls = await _controlRepo.GetByFrameworkAsync(fw.Id, cancellationToken);
            allControls.AddRange(fwControls);
        }

        var findings = await _findingRepo.GetAllAsync(cancellationToken);
        var caps = await _capRepo.GetOverdueAsync(DateTime.UtcNow, cancellationToken);

        var frameworkScores = frameworks.Select(fw =>
        {
            var ctrls = allControls.Where(c => c.FrameworkId == fw.Id).ToList();
            return new FrameworkScoreDto
            {
                Name = fw.Name, Code = fw.Code, TotalControls = ctrls.Count,
                CompliantControls = ctrls.Count(c => c.Status == ControlStatus.Compliant),
                Score = ctrls.Count > 0 ? Math.Round((double)ctrls.Count(c => c.Status == ControlStatus.Compliant) / ctrls.Count * 100, 1) : 0
            };
        }).ToList();

        return new ComplianceDashboardDto
        {
            TotalFrameworks = frameworks.Count,
            TotalControls = allControls.Count,
            CompliantControls = allControls.Count(c => c.Status == ControlStatus.Compliant),
            NonCompliantControls = allControls.Count(c => c.Status == ControlStatus.NonCompliant),
            InProgressControls = allControls.Count(c => c.Status == ControlStatus.InProgress),
            OpenFindings = findings.Count(f => f.Status is AuditFindingStatus.Open or AuditFindingStatus.InProgress),
            CriticalFindings = findings.Count(f => f.Severity == AuditFindingSeverity.Critical && f.Status != AuditFindingStatus.Closed),
            OverdueCAPs = caps.Count,
            OverallComplianceScore = allControls.Count > 0
                ? Math.Round((double)allControls.Count(c => c.Status == ControlStatus.Compliant) / allControls.Count * 100, 1)
                : 0,
            FrameworkScores = frameworkScores,
            RecentFindings = findings.OrderByDescending(f => f.IdentifiedAt).Take(10).Select(f => new FindingDto
            {
                Id = f.Id, ControlId = f.ControlId, Title = f.Title, Severity = f.Severity.ToString(),
                Status = f.Status.ToString(), IdentifiedAt = f.IdentifiedAt
            }).ToList()
        };
    }

    private Guid TenantId()
        => _httpContextAccessor.HttpContext?.Items["TenantId"] as Guid? is { } tenantId && tenantId != Guid.Empty
            ? tenantId
            : throw new InvalidOperationException("Tenant context is required.");
}
