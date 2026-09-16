using AegisCore.API.Authorization;
using AegisCore.Domain;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

/// <summary>
/// Faculty headcount, kept separate from student figures so HR and department views can reuse it.
/// </summary>
[ApiController]
[Route("api/faculty")]
public sealed class FacultySummaryController(AegisDbContext db) : ControllerBase
{
    // Ranks that carry a teaching load. Dean, Registrar and Administrator are office-holders and
    // are reported in byDesignation but excluded from the teaching count used for the ratio.
    private static readonly Designation[] TeachingRanks =
        [Designation.Professor, Designation.AssociateProfessor, Designation.AssistantProfessor, Designation.Lecturer, Designation.TeachingAssistant];

    [HttpGet("summary")]
    [RequirePermission("Students", "Read")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        // The Faculty table has no leaving date. A faculty member is "active" unless the user
        // account that shares their id has been deactivated or suspended; rows with no account
        // (imported staff who never signed in) count as active.
        var rows = await db.Faculties.AsNoTracking()
            .GroupJoin(db.Users.AsNoTracking(), f => f.Id, u => u.Id, (f, users) => new { f.Designation, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.Designation, userStatus = u == null ? (UserStatus?)null : u.Status })
            .Select(x => new { x.Designation, active = x.userStatus == null || x.userStatus == UserStatus.Active })
            .GroupBy(x => new { x.Designation, x.active })
            .Select(g => new { g.Key.Designation, g.Key.active, count = g.Count() })
            .ToListAsync(ct);

        var byDesignation = Enum.GetValues<Designation>().ToDictionary(
            d => char.ToLowerInvariant(d.ToString()[0]) + d.ToString()[1..],
            d => rows.Where(r => r.Designation == d && r.active).Sum(r => r.count));

        return Ok(new
        {
            total = rows.Sum(r => r.count),
            active = rows.Where(r => r.active).Sum(r => r.count),
            teaching = rows.Where(r => r.active && TeachingRanks.Contains(r.Designation)).Sum(r => r.count),
            byDesignation,
            asOf = DateTime.UtcNow
        });
    }
}
