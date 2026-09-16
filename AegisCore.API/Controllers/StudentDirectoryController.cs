using AegisCore.API.Authorization;
using AegisCore.Domain;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/students")]
public sealed class StudentDirectoryController(AegisDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Students", "Read")]
    public async Task<IActionResult> List(CancellationToken ct, int page = 1)
    {
        if (page < 1 || page > 100000) return BadRequest();
        var query = db.Students.Where(s => !s.DirectoryOptOut);
        return Ok(new { total = await query.CountAsync(ct), page,
            items = await query.OrderBy(s => s.LastName).ThenBy(s => s.Id).Skip((page - 1) * 50).Take(50)
                .Select(s => new { s.Id, s.FirstName, s.LastName, s.UniversityId, status = s.EnrollmentStatus.ToString() }).ToListAsync(ct) });
    }

    /// <summary>
    /// Headcount by enrolment status for the whole tenant. Unlike the directory listing this
    /// counts opted-out students too: directory opt-out is a privacy preference, not a status.
    /// </summary>
    [HttpGet("summary")]
    [RequirePermission("Students", "Read")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var counts = await db.Students.AsNoTracking()
            .GroupBy(s => s.EnrollmentStatus)
            .Select(g => new { status = g.Key, count = g.Count() })
            .ToListAsync(ct);
        var byStatus = Enum.GetValues<EnrollmentStatus>().ToDictionary(
            status => char.ToLowerInvariant(status.ToString()[0]) + status.ToString()[1..],
            status => counts.SingleOrDefault(c => c.status == status)?.count ?? 0);
        return Ok(new
        {
            enrolled = byStatus["active"],
            total = counts.Sum(c => c.count),
            byStatus,
            asOf = DateTime.UtcNow
        });
    }
}
