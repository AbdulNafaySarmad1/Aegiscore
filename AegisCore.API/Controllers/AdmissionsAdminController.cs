using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.Application.Admissions;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

/// <summary>
/// Staff-facing admissions configuration and applicant pipeline management (<c>/admin/admissions</c>).
/// Every action is gated by <see cref="RequirePermissionAttribute"/> — never role attributes.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admissions")]
public sealed class AdmissionsAdminController(
    IAdmissionConfigService config,
    IAdmissionsAdminService admin) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    // ---- Intakes ----

    [RequirePermission("Admissions", "Read")]
    [HttpGet("intakes")]
    public async Task<IReadOnlyList<AdmissionIntake>> ListIntakes(CancellationToken ct)
        => await config.ListIntakesAsync(ct);

    [RequirePermission("Admissions", "Configure")]
    [HttpPost("intakes")]
    public async Task<ActionResult<AdmissionIntake>> CreateIntake([FromBody] IntakeInput input, CancellationToken ct)
        => Ok(await config.CreateIntakeAsync(input, ct));

    [RequirePermission("Admissions", "Configure")]
    [HttpPatch("intakes/{intakeId:guid}/status")]
    public async Task<ActionResult<AdmissionIntake>> SetIntakeStatus(Guid intakeId, [FromBody] IntakeStatusRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await config.UpdateIntakeStatusAsync(intakeId, request.Status, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // ---- Program code + document config ----

    [RequirePermission("Admissions", "Configure")]
    [HttpPut("config/programs/{programId:guid}/code")]
    public async Task<IActionResult> SetProgramCode(Guid programId, [FromBody] ProgramCodeRequest request, CancellationToken ct)
    {
        try
        {
            await config.SetProgramCodeAsync(programId, request.Code, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [RequirePermission("Admissions", "Read")]
    [HttpGet("config/documents")]
    public async Task<IReadOnlyList<AdmissionDocumentRequirement>> ListDocumentRequirements([FromQuery] Guid intakeId, [FromQuery] Guid programId, CancellationToken ct)
        => await config.ListDocumentRequirementsAsync(intakeId, programId, ct);

    [RequirePermission("Admissions", "Configure")]
    [HttpPost("config/documents")]
    public async Task<ActionResult<AdmissionDocumentRequirement>> UpsertDocumentRequirement([FromBody] DocumentRequirementInput input, CancellationToken ct)
        => Ok(await config.UpsertDocumentRequirementAsync(input, ct));

    [RequirePermission("Admissions", "Configure")]
    [HttpDelete("config/documents/{requirementId:guid}")]
    public async Task<IActionResult> DeleteDocumentRequirement(Guid requirementId, CancellationToken ct)
    {
        await config.DeleteDocumentRequirementAsync(requirementId, ct);
        return NoContent();
    }

    // ---- Applicant pipeline ----

    [RequirePermission("Admissions", "Read")]
    [HttpGet("applicants")]
    public async Task<IReadOnlyList<AdmissionApplicantRow>> ListApplicants(
        [FromQuery] Guid? intakeId, [FromQuery] Guid? programId, [FromQuery] ApplicantStatus? status, CancellationToken ct)
        => await admin.ListAsync(intakeId, programId, status, ct);

    [RequirePermission("Admissions", "Read")]
    [AegisCore.API.Authorization.RequireFerpaAudit("Admission", "applicantId")]
    [HttpGet("applicants/{applicantId:guid}")]
    public async Task<ActionResult<AdmissionApplicantDetail>> GetApplicant(Guid applicantId, CancellationToken ct)
        => await admin.GetAsync(applicantId, ct) is { } detail ? Ok(detail) : NotFound();

    [RequirePermission("Admissions", "Read")]
    [AegisCore.API.Authorization.RequireFerpaAudit("Admission", "applicantId")]
    [HttpGet("applicants/{applicantId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> DownloadDocument(Guid applicantId, Guid documentId, CancellationToken ct)
    {
        var download = await admin.DownloadDocumentAsync(applicantId, documentId, ct);
        if (download is null) return NotFound();
        return File(download.Content, download.ContentType, download.FileName);
    }

    [RequirePermission("Admissions", "Approve")]
    [AegisCore.API.Authorization.RequireFerpaAudit("Admission", "applicantId")]
    [HttpPost("applicants/{applicantId:guid}/documents/{documentId:guid}/review")]
    public async Task<IActionResult> ReviewDocument(Guid applicantId, Guid documentId, [FromBody] DocumentReviewRequest request, CancellationToken ct)
    {
        try
        {
            await admin.ReviewDocumentAsync(applicantId, documentId, request.Approve, request.RejectionReason, CurrentUserId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [RequirePermission("Admissions", "Approve")]
    [AegisCore.API.Authorization.RequireFerpaAudit("Admission", "applicantId")]
    [HttpPatch("applicants/{applicantId:guid}/status")]
    public async Task<IActionResult> Transition(Guid applicantId, [FromBody] ApplicantTransitionRequest request, CancellationToken ct)
    {
        try
        {
            await admin.TransitionAsync(applicantId, request.Status, request.Note, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission("Admissions", "Approve")]
    [HttpPost("applicants/bulk-status")]
    public async Task<ActionResult<object>> BulkTransition([FromBody] BulkTransitionRequest request, CancellationToken ct)
    {
        var moved = await admin.BulkTransitionAsync(request.ApplicantIds, request.Status, request.Note, ct);
        return Ok(new { moved, requested = request.ApplicantIds.Count });
    }

    public sealed record IntakeStatusRequest(IntakeStatus Status);
    public sealed record ProgramCodeRequest(string Code);
    public sealed record DocumentReviewRequest(bool Approve, string? RejectionReason);
    public sealed record ApplicantTransitionRequest(ApplicantStatus Status, string? Note);
    public sealed record BulkTransitionRequest(IReadOnlyList<Guid> ApplicantIds, ApplicantStatus Status, string? Note);
}
