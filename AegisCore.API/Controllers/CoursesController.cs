using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AegisCore.API.Controllers;

/// <summary>
/// Courses, as the people who teach them need to see them.
///
/// The attendance screen and the marks screen both open by asking for "my courses" — and nothing
/// answered: no endpoint was mapped at that address, so both screens began with "Request failed
/// (404)" for everyone, and neither could ever list a course to mark a roll or enter a grade for.
/// </summary>
[ApiController]
[Route("api/courses")]
[Authorize]
public sealed class CoursesController(AegisDbContext db) : ControllerBase
{
    /// <summary>
    /// The courses the signed-in faculty member is assigned to teach. An administrator who teaches
    /// nothing gets an empty list, not an error: having no courses is an answer.
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { error = "Sign in again; this request carried no identity." });

        // A faculty account is created with the same id as its faculty record, as a student account
        // is with its student record.
        var courses = await db.CourseAssignments.AsNoTracking()
            .Where(assignment => assignment.FacultyId == userId)
            .Select(assignment => new
            {
                id = assignment.Course.Id,
                code = assignment.Course.Code,
                title = assignment.Course.Title,
                creditHours = assignment.Course.CreditHours,
            })
            .Distinct()
            .OrderBy(course => course.code)
            .ToListAsync(ct);

        return Ok(courses);
    }
}
