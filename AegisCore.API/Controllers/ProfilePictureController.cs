using System.Security.Claims;
using AegisCore.Application.ProfilePictures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController][Authorize][Route("api/profile-pictures")]
public class ProfilePictureController : ControllerBase
{
    private readonly IProfilePictureService _svc;
    public ProfilePictureController(IProfilePictureService svc) => _svc = svc;

    [HttpPost("upload"), RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value ?? User.FindFirst("tid")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId)
            || !Guid.TryParse(tenantIdClaim, out var tenantId)
            || userId == Guid.Empty
            || tenantId == Guid.Empty)
        {
            return Unauthorized(new { error = "missing_or_invalid_identity" });
        }

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        ms.Position = 0;
        var contentType = Request.ContentType ?? "image/jpeg";
        var result = await _svc.UploadAsync(ms, contentType, userId, tenantId, ct);
        return result.Accepted
            ? Ok(result)
            : BadRequest(new { error = result.RejectionReason, faceValidated = result.FaceValidated, editingDetected = result.EditingDetected });
    }

    [HttpGet("user/{userId:guid}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(Guid userId, CancellationToken ct)
    {
        var stream = await _svc.GetThumbnailAsync(userId, ct);
        if (stream == null) return NotFound(new { error = "No profile picture found." });
        return File(stream, "image/jpeg");
    }

    [HttpGet("user/{userId:guid}/full")]
    public async Task<IActionResult> GetFull(Guid userId, CancellationToken ct)
    {
        var stream = await _svc.GetFullAsync(userId, ct);
        if (stream == null) return NotFound(new { error = "No profile picture found." });
        return File(stream, "image/jpeg");
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetRecord(Guid userId, CancellationToken ct)
    {
        var record = await _svc.GetRecordAsync(userId, ct);
        return record == null ? NotFound(new { error = "No profile picture found." }) : Ok(new
        {
            record.Id, record.UserId, record.FaceValidated, record.EditingDetected,
            record.UploadedAt, fullUrl = record.FullStorageUrl, thumbnailUrl = record.ThumbnailStorageUrl
        });
    }
}
