using System.Security.Claims;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize(Roles = "Faculty,HOD,Dean,Admin,SuperAdmin")]
public sealed class FacultyCoursesController(AegisDbContext db) : ControllerBase
{
    [HttpGet("my")]
    public async Task<IActionResult> MyCourses(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var facultyId) || facultyId == Guid.Empty)
            return Unauthorized(new { error = "Faculty identity is missing from the authenticated session." });

        var courses = await db.Timetables
            .AsNoTracking()
            .Where(t => t.FacultyId == facultyId)
            .Select(t => new
            {
                id = t.CourseId,
                code = t.Course.Code,
                title = t.Course.Title
            })
            .Distinct()
            .OrderBy(c => c.code)
            .ToListAsync(ct);

        return Ok(courses);
    }
}
