using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.Application.Admissions;
using AegisCore.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

/// <summary>
/// Post-admission migration: admit → Student + roll number + institutional email, and the
/// Kanban admissions pipeline board.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admissions")]
public sealed class AdmissionsMigrationController(
    IAdmissionDecisionService decisions,
    IAdmissionPipelineService pipeline) : ControllerBase
{
    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [RequirePermission("Admissions", "Admit")]
    [AegisCore.API.Authorization.RequireFerpaAudit("Admission", "applicantId")]
    [HttpPost("applicants/{applicantId:guid}/admit")]
    public async Task<ActionResult<AdmitResult>> Admit(Guid applicantId, [FromBody] AdmitRequest? request, CancellationToken ct)
    {
        try
        {
            return Ok(await decisions.AdmitAsync(applicantId, CurrentUserId, request?.ManualInstitutionalEmail, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission("Admissions", "Read")]
    [HttpGet("pipeline")]
    public async Task<ActionResult<PipelineBoard>> Board([FromQuery] Guid? intakeId, CancellationToken ct)
        => Ok(await pipeline.GetBoardAsync(intakeId, ct));

    [RequirePermission("Admissions", "Approve")]
    [HttpPost("pipeline/move")]
    public async Task<IActionResult> Move([FromBody] PipelineMoveRequest request, CancellationToken ct)
    {
        try
        {
            await pipeline.MoveAsync(request.ApplicantId, request.Column, CurrentUserId, request.ManualInstitutionalEmail, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission("Admissions", "Approve")]
    [HttpPost("pipeline/bulk-move")]
    public async Task<ActionResult<object>> BulkMove([FromBody] PipelineBulkMoveRequest request, CancellationToken ct)
    {
        var moved = await pipeline.BulkMoveAsync(request.ApplicantIds, request.Column, CurrentUserId, ct);
        return Ok(new { moved, requested = request.ApplicantIds.Count });
    }

    public sealed record AdmitRequest(string? ManualInstitutionalEmail);
    public sealed record PipelineMoveRequest(Guid ApplicantId, string Column, string? ManualInstitutionalEmail);
    public sealed record PipelineBulkMoveRequest(IReadOnlyList<Guid> ApplicantIds, string Column);
}

/// <summary>Admin-selected institutional email provisioning mode (Google Workspace / Microsoft 365 / Manual).</summary>
[ApiController]
[Authorize]
[Route("api/admin/settings/email-provisioning")]
public sealed class EmailProvisioningSettingsController(IEmailProvisioningConfigService config) : ControllerBase
{
    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [RequirePermission("Settings", "Read")]
    [HttpGet]
    public async Task<ActionResult<EmailProvisioningConfigView>> Get(CancellationToken ct)
        => await config.GetAsync(ct) is { } view ? Ok(view) : NoContent();

    [RequirePermission("Settings", "Write")]
    [HttpPut]
    public async Task<ActionResult<EmailProvisioningConfigView>> Set([FromBody] EmailProvisioningConfigInput input, CancellationToken ct)
    {
        try
        {
            return Ok(await config.SetAsync(input, CurrentUserId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
