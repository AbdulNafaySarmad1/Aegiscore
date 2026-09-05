using AegisCore.Application.Curriculum;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/curriculum")]
[Authorize]
public sealed class CurriculumController(ICurriculumService curriculum) : ControllerBase
{
    [HttpGet("versions/{versionId:guid}/graph")]
    public async Task<IActionResult> Graph(Guid versionId, CancellationToken ct)
        => Ok(await curriculum.GetGraphAsync(versionId, ct));

    [HttpGet("workload/can-assign")]
    public IActionResult CanAssign([FromQuery] int existingHours, [FromQuery] int existingSections, [FromQuery] int newHours, [FromQuery] int maxHours = 12, [FromQuery] int maxSections = 4)
    {
        var existing = Enumerable.Range(0, existingSections)
            .Select(i => new FacultyWorkload { CourseId = Guid.NewGuid(), HoursPerWeek = i == 0 ? existingHours : 0 })
            .ToList();
        return Ok(new { allowed = curriculum.CanAssignWorkload(existing, newHours, maxHours, maxSections) });
    }

    [HttpPost("attendance/offline-sync")]
    [AegisCore.API.Authorization.RequirePermission("Grades", "Write")]
    public async Task<IActionResult> OfflineSync([FromBody] OfflineAttendanceEvent[]? events, CancellationToken ct)
    {
        if (events is null) return BadRequest("Attendance events are required.");
        if (events.Length > 500) return BadRequest("A maximum of 500 attendance events may be synced at once.");
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor)) return Unauthorized();
        foreach (var entry in events) entry.MarkedBy = actor;
        return Ok(new { synced = await curriculum.SyncOfflineAttendanceAsync(events, ct) });
    }
}
