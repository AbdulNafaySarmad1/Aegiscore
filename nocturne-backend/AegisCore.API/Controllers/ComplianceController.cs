using AegisCore.Application.Compliance;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/compliance")]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceService _complianceService;
    private readonly IComplianceFrameworkRepository _frameworkRepo;
    private readonly IComplianceControlRepository _controlRepo;
    private readonly IAuditFindingRepository _findingRepo;
    private readonly ILogger<ComplianceController> _logger;

    public ComplianceController(
        IComplianceService complianceService,
        IComplianceFrameworkRepository frameworkRepo,
        IComplianceControlRepository controlRepo,
        IAuditFindingRepository findingRepo,
        ILogger<ComplianceController> logger)
    {
        _complianceService = complianceService;
        _frameworkRepo = frameworkRepo;
        _controlRepo = controlRepo;
        _findingRepo = findingRepo;
        _logger = logger;
    }

    [HttpPost("frameworks")]
    public async Task<IActionResult> RegisterFramework([FromBody] RegisterFrameworkRequest request, CancellationToken cancellationToken)
    {
        var framework = await _complianceService.RegisterFrameworkAsync(request.Name, request.Code, request.Description, cancellationToken);
        return Ok(new ComplianceFrameworkDto { Id = framework.Id, Name = framework.Name, Code = framework.Code, Status = framework.Status.ToString() });
    }

    [HttpGet("frameworks")]
    public async Task<IActionResult> ListFrameworks(CancellationToken cancellationToken)
    {
        var frameworks = await _frameworkRepo.GetActiveAsync(cancellationToken);
        var response = new List<ComplianceFrameworkDto>();

        foreach (var fw in frameworks)
        {
            var controls = await _controlRepo.GetByFrameworkAsync(fw.Id, cancellationToken);
            response.Add(new ComplianceFrameworkDto
            {
                Id = fw.Id, Name = fw.Name, Code = fw.Code, Description = fw.Description,
                Version = fw.Version, Status = fw.Status.ToString(), ControlCount = controls.Count,
                CompliantCount = controls.Count(c => c.Status == ControlStatus.Compliant),
                NonCompliantCount = controls.Count(c => c.Status == ControlStatus.NonCompliant),
                ComplianceScore = controls.Count > 0 ? Math.Round((double)controls.Count(c => c.Status == ControlStatus.Compliant) / controls.Count * 100, 1) : 0
            });
        }

        return Ok(new { total = response.Count, frameworks = response });
    }

    [HttpPost("controls")]
    public async Task<IActionResult> AddControl([FromBody] AddControlRequest request, CancellationToken cancellationToken)
    {
        var control = await _complianceService.AddControlAsync(
            request.FrameworkId, request.ControlCode, request.Title, request.Description,
            request.OwnerUserId, request.OwnerName, cancellationToken);

        return Ok(new ControlDto { Id = control.Id, ControlCode = control.ControlCode, Title = control.Title, Status = control.Status.ToString() });
    }

    [HttpGet("frameworks/{frameworkId:guid}/controls")]
    public async Task<IActionResult> ListControls(Guid frameworkId, CancellationToken cancellationToken)
    {
        var controls = await _controlRepo.GetByFrameworkAsync(frameworkId, cancellationToken);
        var response = controls.Select(c => new ControlDto
        {
            Id = c.Id, FrameworkId = c.FrameworkId, ControlCode = c.ControlCode, Title = c.Title,
            Description = c.Description, Status = c.Status.ToString(), OwnerName = c.OwnerName,
            ReviewFrequency = c.ReviewFrequency.ToString(), NextReviewDate = c.NextReviewDate,
            EvidenceCount = c.Evidences?.Count ?? 0, FindingCount = c.Findings?.Count ?? 0
        });

        return Ok(new { total = response.Count(), controls = response });
    }

    [HttpPut("controls/{controlId:guid}/status")]
    public async Task<IActionResult> UpdateControlStatus(Guid controlId, [FromBody] UpdateControlStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ControlStatus>(request.Status, true, out var status))
            return BadRequest(new { error = "invalid_status" });

        var control = await _complianceService.UpdateControlStatusAsync(controlId, status, request.Notes, cancellationToken);
        return Ok(new ControlDto { Id = control.Id, ControlCode = control.ControlCode, Status = control.Status.ToString() });
    }

    [HttpPost("findings")]
    public async Task<IActionResult> ReportFinding([FromBody] ReportFindingRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AuditFindingSeverity>(request.Severity, true, out var severity))
            return BadRequest(new { error = "invalid_severity" });

        var finding = await _complianceService.ReportFindingAsync(
            request.ControlId, request.Title, request.Description, severity,
            Guid.Empty, "Auditor", cancellationToken);

        return Ok(new FindingDto { Id = finding.Id, Title = finding.Title, Severity = finding.Severity.ToString(), Status = finding.Status.ToString() });
    }

    [HttpGet("findings")]
    public async Task<IActionResult> ListFindings([FromQuery] string? status, [FromQuery] string? severity, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Entities.AuditFinding> findings;

        if (!string.IsNullOrWhiteSpace(severity) && Enum.TryParse<AuditFindingSeverity>(severity, true, out var sev))
            findings = await _findingRepo.GetBySeverityAsync(sev, cancellationToken);
        else if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AuditFindingStatus>(status, true, out var st))
            findings = await _findingRepo.GetByStatusAsync(st, cancellationToken);
        else
            findings = await _findingRepo.GetAllAsync(cancellationToken);

        var response = findings.Select(f => new FindingDto
        {
            Id = f.Id, ControlId = f.ControlId, Title = f.Title, Description = f.Description,
            Severity = f.Severity.ToString(), Status = f.Status.ToString(),
            IdentifiedByName = f.IdentifiedByName, IdentifiedAt = f.IdentifiedAt,
            TargetResolutionDate = f.TargetResolutionDate
        });

        return Ok(new { total = response.Count(), findings = response });
    }

    [HttpPost("caps")]
    public async Task<IActionResult> CreateCAP([FromBody] CreateCAPRequest request, CancellationToken cancellationToken)
    {
        var cap = await _complianceService.CreateActionPlanAsync(
            request.FindingId, request.Description, request.AssigneeId, request.AssigneeName, request.DueDate, cancellationToken);

        return Ok(new CAPDto { Id = cap.Id, Description = cap.Description, AssignedToName = cap.AssignedToName, Status = cap.Status.ToString(), DueDate = cap.DueDate });
    }

    [HttpPut("caps/{capId:guid}/complete")]
    public async Task<IActionResult> CompleteCAP(Guid capId, [FromBody] CompleteCAPRequest request, CancellationToken cancellationToken)
    {
        var cap = await _complianceService.CompleteActionPlanAsync(capId, request.CompletionNotes, cancellationToken);
        return Ok(new CAPDto { Id = cap.Id, Status = cap.Status.ToString(), CompletedAt = cap.CompletedAt });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] Guid? frameworkId, CancellationToken cancellationToken)
    {
        var dashboard = await _complianceService.GetDashboardAsync(frameworkId, cancellationToken);
        return Ok(dashboard);
    }
}
