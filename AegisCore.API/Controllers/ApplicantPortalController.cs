using System.Security.Claims;
using AegisCore.Application.Admissions;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Admissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

/// <summary>
/// Public applicant portal (<c>/apply</c>). Auth is a dedicated JWT scheme with audience
/// <c>applicant</c> — these tokens cannot reach any internal AegisCore route.
/// </summary>
[ApiController]
[Route("api/applicant")]
public sealed class ApplicantPortalController(IApplicantPortalService portal, IAdmissionCatalogService catalog) : ControllerBase
{
    private Guid ApplicantProfileId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id
            : Guid.Empty;

    [AllowAnonymous]
    [HttpGet("intakes/open")]
    public async Task<ActionResult<IReadOnlyList<OpenIntakeView>>> OpenIntakes(CancellationToken ct)
        => Ok(await catalog.GetOpenIntakesAsync(ct));

    [AllowAnonymous]
    [HttpPost("auth/signup")]
    public async Task<ActionResult<ApplicantSignupResult>> Signup([FromBody] ApplicantSignupRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await portal.SignupAsync(request.Email, request.IntakeId, request.ProgramId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("auth/verify")]
    public async Task<ActionResult<ApplicantVerifyResult>> Verify([FromBody] ApplicantVerifyRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await portal.VerifyAsync(request.Email, request.Otp, request.IntakeId, request.ProgramId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = ApplicantAuth.Scheme)]
    [HttpGet("profile")]
    public async Task<ActionResult<ApplicantProfileView>> GetProfile(CancellationToken ct)
        => await Guarded(() => portal.GetProfileAsync(ApplicantProfileId, ct));

    [Authorize(AuthenticationSchemes = ApplicantAuth.Scheme)]
    [HttpPut("profile")]
    public async Task<ActionResult<ApplicantProfileView>> UpdateProfile([FromBody] ApplicantPersonalDetails details, CancellationToken ct)
        => await Guarded(() => portal.UpdatePersonalDetailsAsync(ApplicantProfileId, details, ct));

    [Authorize(AuthenticationSchemes = ApplicantAuth.Scheme)]
    [HttpPost("documents/{documentType}")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult<ApplicantDocumentUploadResult>> UploadDocument(string documentType, IFormFile file, CancellationToken ct)
    {
        if (!Enum.TryParse<AdmissionDocumentKind>(documentType, true, out var kind))
        {
            return BadRequest(new { error = "Unknown document type." });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "A file is required." });
        }

        await using var stream = file.OpenReadStream();
        return await Guarded(() => portal.UploadDocumentAsync(ApplicantProfileId, kind, stream, file.FileName, file.ContentType ?? string.Empty, file.Length, ct));
    }

    [Authorize(AuthenticationSchemes = ApplicantAuth.Scheme)]
    [HttpPost("photo")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult<ApplicantDocumentUploadResult>> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "A photo is required." });
        }

        await using var stream = file.OpenReadStream();
        return await Guarded(() => portal.UploadPhotoAsync(ApplicantProfileId, stream, file.FileName, file.ContentType ?? string.Empty, file.Length, ct));
    }

    [Authorize(AuthenticationSchemes = ApplicantAuth.Scheme)]
    [HttpGet("consent")]
    public async Task<ActionResult<ApplicantConsentTexts>> ConsentTexts(CancellationToken ct)
        => await Guarded(() => portal.GetConsentTextsAsync(ApplicantProfileId, ct));

    [Authorize(AuthenticationSchemes = ApplicantAuth.Scheme)]
    [HttpPost("submit")]
    public async Task<ActionResult<ApplicantProfileView>> Submit([FromBody] ApplicantSubmission? submission, CancellationToken ct)
        => await Guarded(() => portal.SubmitAsync(
            ApplicantProfileId,
            (submission ?? new ApplicantSubmission(false)) with { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() },
            ct));

    private async Task<ActionResult<T>> Guarded<T>(Func<Task<T>> action)
    {
        if (ApplicantProfileId == Guid.Empty)
        {
            return Unauthorized(new { error = "Invalid applicant session." });
        }

        try
        {
            return Ok(await action());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public sealed record ApplicantSignupRequest(string Email, Guid IntakeId, Guid ProgramId);
    public sealed record ApplicantVerifyRequest(string Email, string Otp, Guid IntakeId, Guid ProgramId);
}
