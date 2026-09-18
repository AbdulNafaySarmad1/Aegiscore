using System.Security.Claims;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Foundation;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

/// <summary>
/// How a staff account gets a password. The wizard sets the first administrator's; admitted
/// students set their own through the activation link; every other local account was created
/// by <c>POST /api/identity/users</c> with no password and no way to obtain one. An administrator
/// sets an initial password here (audited), and the person changes it themselves afterwards.
/// </summary>
[ApiController]
[Authorize]
public sealed class PasswordController(AegisDbContext db, IPasswordHashingService passwords, IAuditService audit) : ControllerBase
{
    public const int MinimumLength = 16;
    public const int MaximumLength = 128;

    [HttpPost("api/identity/users/{id:guid}/password")]
    [Authorization.RequirePermission("System", "Write")]
    public async Task<IActionResult> SetPassword(Guid id, [FromBody] SetPasswordRequest request, CancellationToken ct)
    {
        if (!AcceptablePassword(request.Password, out var why)) return BadRequest(new { error = why });
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound(new { error = "User was not found." });
        if (user.AuthProvider != AegisCore.Domain.AuthProvider.Local)
            return Conflict(new { error = $"This account signs in through {user.AuthProvider}; it has no local password." });

        user.PasswordHash = passwords.HashPassword(request.Password);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(new AuditEntry(ActorId(), User.FindFirstValue(ClaimTypes.Email), "identity.password.set", "identity", "User",
            user.Id.ToString(), null, "Initial password set by an administrator.", HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent, null), ct);
        return Ok(new { message = $"Password set for {user.Username}. They can sign in with it now." });
    }

    [HttpPost("api/auth/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var actorId = ActorId();
        if (actorId is null) return Unauthorized();
        if (!AcceptablePassword(request.NewPassword, out var why)) return BadRequest(new { error = why });
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == actorId, ct);
        if (user is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(user.PasswordHash) || !passwords.VerifyPassword(request.CurrentPassword ?? string.Empty, user.PasswordHash))
            return BadRequest(new { error = "The current password is not correct." });
        if (request.CurrentPassword == request.NewPassword)
            return BadRequest(new { error = "The new password must differ from the current one." });

        user.PasswordHash = passwords.HashPassword(request.NewPassword);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(new AuditEntry(actorId, user.Email, "identity.password.changed", "identity", "User",
            user.Id.ToString(), null, null, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent, null), ct);
        return Ok(new { message = "Password changed." });
    }

    private Guid? ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static bool AcceptablePassword(string? password, out string why)
    {
        why = string.Empty;
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumLength || password.Length > MaximumLength)
        {
            why = $"Use a password of {MinimumLength} to {MaximumLength} characters.";
            return false;
        }
        return true;
    }

    public sealed record SetPasswordRequest(string Password);
    public sealed record ChangePasswordRequest(string? CurrentPassword, string NewPassword);
}
