using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.StudentLifecycle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController, Authorize, Route("api/graduation")]
public sealed class GraduationController(AegisDbContext db, IAuthorizationService authorization) : ControllerBase
{
    private Guid Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    [HttpGet("{studentId:guid}")]
    public async Task<IActionResult> Progress(Guid studentId, CancellationToken ct)
    {
        if (Actor != studentId && !(await authorization.AuthorizeAsync(User, null, new PermissionRequirement("Graduation", "Approve"))).Succeeded) return Forbid();
        try { return Ok(await new GraduationService(db).EvaluateAsync(studentId, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{studentId:guid}/clearances/{department}"), RequirePermission("Graduation", "Approve")]
    public async Task<IActionResult> Clear(Guid studentId, string department, CancellationToken ct)
    {
        if (!IsAdministrator) return Forbid();
        try { await new GraduationService(db).ClearAsync(studentId, department, Actor, ct); return Ok(new { cleared = true }); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("{studentId:guid}/approve"), RequirePermission("Graduation", "Approve")]
    public async Task<IActionResult> Approve(Guid studentId, CancellationToken ct)
    {
        if (!IsAdministrator) return Forbid();
        try { return Ok(await new GraduationService(db).ApproveAsync(studentId, Actor, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    private bool IsAdministrator => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
}
