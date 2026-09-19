using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Identity;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.API.Authorization;
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
    IConnectionMultiplexer redis,
    ICasbinPolicySyncService? casbin = null,
    IExternalSignInService? signInMethods = null) : ControllerBase
{
    private const string RefreshTokenKeyPrefix = SessionTokenIssuer.RefreshTokenKeyPrefix;

    // The same tail as the SSO callback: whichever door the person came through, the token is built here.
    private SessionTokenIssuer Issuer => new(db, tokens, redis, configuration, casbin);

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

        // A university that chose SSO without Local in the wizard has turned this form off; saying
        // so is kinder than "invalid credentials" to someone whose password is perfectly good.
        if (signInMethods is not null)
        {
            var methods = await signInMethods.GetLoginMethodsAsync(tenantId, ct);
            if (!methods.LocalEnabled)
                return StatusCode(403, new
                {
                    error = "Password sign-in is turned off for this university. Sign in with " + string.Join(" or ", methods.Providers.Select(p => p.DisplayName)) + ".",
                    code = "local_sign_in_disabled"
                });
        }

        var matchingUsers = await db.Users.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .Where(x => x.TenantId == tenantId && x.Email == email)
            .Take(2)
            .ToListAsync(ct);
        var user = matchingUsers.Count == 1 ? matchingUsers[0] : null;

        if (user is null || user.Status != AegisCore.Domain.UserStatus.Active ||
            string.IsNullOrWhiteSpace(user.PasswordHash) || !passwords.VerifyPassword(request.Password, user.PasswordHash))
        {
            await bruteForce.RecordFailedAttemptAsync(email);
            return Unauthorized(new { error = "Invalid credentials." });
        }

        await bruteForce.ClearFailedAttemptsAsync(email);
        var issued = await Issuer.IssueAsync(user, ct);
        return Ok(new { accessToken = issued.AccessToken, refreshToken = issued.RefreshToken, tokenType = issued.TokenType, expiresIn = issued.ExpiresIn });
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

        var issuer = Issuer;
        var user = await issuer.LoadForSignInAsync(tenantId, userId, ct);
        if (user is null || user.Status != AegisCore.Domain.UserStatus.Active)
            return Unauthorized(new { error = "Invalid or expired refresh token." });

        var issued = await issuer.IssueAsync(user, ct);
        return Ok(new { accessToken = issued.AccessToken, refreshToken = issued.RefreshToken, tokenType = issued.TokenType, expiresIn = issued.ExpiresIn });
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
