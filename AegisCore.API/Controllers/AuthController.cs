using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AegisCore.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(
    AegisDbContext db,
    IPasswordHashingService passwords,
    IJwtTokenService tokens,
    IConfiguration configuration,
    IBruteForceProtectionService bruteForce,
    IConnectionMultiplexer redis) : ControllerBase
{
    private const string RefreshTokenKeyPrefix = "aegiscore:auth:refresh:token:";
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(7);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(new { error = "Invalid credentials." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await bruteForce.IsLockedOutAsync(email))
            return StatusCode(429, new { error = "Account temporarily locked due to too many failed attempts. Please try again in 15 minutes." });

        // Single-tenant / demo mode: tenant is resolved from configuration.
        // In a multi-tenant deployment, replace this with tenant discovery (e.g.
        // from the user's email domain, a tenant claim in the JWT, or an explicit
        // tenant selection step before login).
        var tenantId = ResolveTenantId();
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

        var accessToken = tokens.GenerateAccessToken(user);
        var refreshToken = tokens.GenerateRefreshToken();
        var familyId = await tokens.GenerateRefreshTokenFamilyAsync(user.Id);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var tokenData = new
        {
            family_id = familyId.ToString(),
            user_id = user.Id.ToString(),
            expires_at = expiresAt.ToString("O")
        };
        await redis.GetDatabase().StringSetAsync($"{RefreshTokenKeyPrefix}{refreshToken}", JsonSerializer.Serialize(tokenData), RefreshTokenTtl);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = expiresAt;
        await db.SaveChangesAsync(ct);

        return Ok(new { accessToken, refreshToken, tokenType = "Bearer", expiresIn = 900 });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new { error = "Refresh token is required." });

        var tenantId = ResolveTenantId();
        if (tenantId == Guid.Empty) return Unauthorized(new { error = "Invalid credentials." });

        var db2 = redis.GetDatabase();
        var tokenKey = $"{RefreshTokenKeyPrefix}{request.RefreshToken}";
        var tokenJson = await db2.StringGetAsync(tokenKey);

        if (!tokenJson.HasValue)
            return Unauthorized(new { error = "Token revoked or reused" });

        using var doc = JsonDocument.Parse(tokenJson.ToString());
        var root = doc.RootElement;
        var familyId = Guid.Parse(root.GetProperty("family_id").GetString()!);
        var userId = Guid.Parse(root.GetProperty("user_id").GetString()!);

        if (!await tokens.IsRefreshTokenFamilyValidAsync(familyId))
        {
            await db2.KeyDeleteAsync(tokenKey);
            return Unauthorized(new { error = "Token revoked or reused" });
        }

        await tokens.InvalidateRefreshTokenFamilyAsync(familyId);
        await db2.KeyDeleteAsync(tokenKey);

        var user = await db.Users.IgnoreQueryFilters()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == userId, ct);

        if (user is null || user.Status != AegisCore.Domain.UserStatus.Active)
            return Unauthorized(new { error = "Invalid or expired refresh token." });

        var accessToken = tokens.GenerateAccessToken(user);
        var newRefreshToken = tokens.GenerateRefreshToken();
        var newFamilyId = await tokens.GenerateRefreshTokenFamilyAsync(user.Id);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var newTokenData = new
        {
            family_id = newFamilyId.ToString(),
            user_id = user.Id.ToString(),
            expires_at = expiresAt.ToString("O")
        };
        await db2.StringSetAsync($"{RefreshTokenKeyPrefix}{newRefreshToken}", JsonSerializer.Serialize(newTokenData), RefreshTokenTtl);

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = expiresAt;
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { accessToken, refreshToken = newRefreshToken, tokenType = "Bearer", expiresIn = 900 });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { error = "Refresh token is required." });

        var db2 = redis.GetDatabase();
        var tokenKey = $"{RefreshTokenKeyPrefix}{request.RefreshToken}";
        var tokenJson = await db2.StringGetAsync(tokenKey);

        if (!tokenJson.HasValue)
            return Ok(new { message = "Logged out." });

        using var doc = JsonDocument.Parse(tokenJson.ToString());
        var familyId = Guid.Parse(doc.RootElement.GetProperty("family_id").GetString()!);

        await tokens.InvalidateRefreshTokenFamilyAsync(familyId);
        await db2.KeyDeleteAsync(tokenKey);

        return Ok(new { message = "Logged out." });
    }

    private Guid ResolveTenantId()
    {
        return Guid.TryParse(configuration["TENANT_ID"], out var configured) ? configured : Guid.Empty;
    }

    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);
}
