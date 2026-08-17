using System.Security.Claims;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/me/profile")]
[Authorize]
public sealed class SelfProfileController(AegisDbContext db) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (UserId == Guid.Empty) return Unauthorized();
        var user = await db.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return NotFound();
        var faculty = await db.Faculties.AsNoTracking().FirstOrDefaultAsync(f => f.Id == UserId, ct);
        var department = faculty is null ? null : await db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == faculty.DepartmentId, ct);
        return Ok(new
        {
            id = user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Username,
            roles = User.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value).Distinct().ToArray(),
            employeeCode = faculty?.EmployeeCode,
            designation = faculty?.Designation.ToString(),
            department = department?.Name,
            departmentId = faculty?.DepartmentId,
            avatarThumbnailUrl = $"/api/profile-pictures/user/{user.Id:D}/thumbnail",
            avatarFullUrl = $"/api/profile-pictures/user/{user.Id:D}/full"
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] SelfProfileUpdate request, CancellationToken ct)
    {
        if (UserId == Guid.Empty) return Unauthorized();
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return NotFound();

        if (request.FirstName is not null)
        {
            var value = request.FirstName.Trim();
            if (value.Length is < 1 or > 100) return BadRequest(new { error = "First name must be 1-100 characters." });
            user.FirstName = value;
        }
        if (request.LastName is not null)
        {
            var value = request.LastName.Trim();
            if (value.Length is < 1 or > 100) return BadRequest(new { error = "Last name must be 1-100 characters." });
            user.LastName = value;
        }

        // Email/username, employee code, designation and department are intentionally not self-editable.
        await db.SaveChangesAsync(ct);
        return Ok(new { success = true, user.FirstName, user.LastName });
    }
}

public sealed record SelfProfileUpdate(string? FirstName, string? LastName);
