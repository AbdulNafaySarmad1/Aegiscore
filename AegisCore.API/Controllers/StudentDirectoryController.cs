using AegisCore.API.Authorization;
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
}
