using AegisCore.Application.MicrosoftEcosystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/microsoft-ecosystem")]
[Authorize]
public class MicrosoftEcosystemController : ControllerBase
{
    [HttpPost("teams/timetable/{timetableEntryId:guid}/provision")]
    public async Task<IActionResult> ProvisionTeams(Guid timetableEntryId, [FromServices] ITeamsProvisioningService service, CancellationToken ct)
        => Ok(await service.ProvisionClassAsync(timetableEntryId, ct));

    [HttpPost("moodle/courses/{courseId:guid}/roster")]
    public async Task<IActionResult> SyncRoster(Guid courseId, [FromServices] IMoodleLmsService service, CancellationToken ct)
        => Ok(await service.SyncRosterAsync(courseId, ct));

    [HttpPost("moodle/courses/{courseId:guid}/grades")]
    public async Task<IActionResult> SyncGrades(Guid courseId, [FromServices] IMoodleLmsService service, CancellationToken ct)
        => Ok(await service.SyncGradePassbackAsync(courseId, ct));

    [HttpGet("powerbi/embed")]
    public async Task<IActionResult> PowerBiEmbed([FromQuery] Guid userId, [FromQuery] string role, [FromServices] IPowerBiEmbeddingService service, CancellationToken ct)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(role))
            return BadRequest("userId and role are required.");

        var isPrivileged = User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!isPrivileged && (!Guid.TryParse(subject, out var subjectId) || subjectId != userId))
            return Forbid();

        return Ok(await service.GetEmbedConfigAsync(userId, role, ct));
    }
}
