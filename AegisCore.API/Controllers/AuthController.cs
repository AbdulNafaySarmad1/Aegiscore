using AegisCore.Application.Common.Interfaces;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(
    AegisDbContext db,
    IPasswordHashingService passwords,
    IJwtTokenService tokens,
    IConfiguration configuration,
    IBruteForceProtectionService bruteForce) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(new { error = "Invalid credentials." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await bruteForce.IsLockedOutAsync(email))
            return StatusCode(429, new { error = "Account temporarily locked due to too many failed attempts. Please try again in 15 minutes." });

        var tenantId = Guid.TryParse(configuration["TENANT_ID"], out var configured) ? configured : Guid.Empty;
        if (tenantId == Guid.Empty) return Unauthorized(new { error = "Invalid credentials." });

        var user = await db.Users.IgnoreQueryFilters()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Email == email, ct);

        if (user is null || user.Status != AegisCore.Domain.UserStatus.Active ||
            string.IsNullOrWhiteSpace(user.PasswordHash) || !passwords.VerifyPassword(request.Password, user.PasswordHash))
        {
            await bruteForce.RecordFailedAttemptAsync(email);
            return Unauthorized(new { error = "Invalid credentials." });
        }

        await bruteForce.ClearFailedAttemptsAsync(email);
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { accessToken = tokens.GenerateAccessToken(user), tokenType = "Bearer", expiresIn = 900 });
    }

    public sealed record LoginRequest(string Email, string Password);
}
