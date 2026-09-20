using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Identity;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.API.Authorization;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Security;
using System.Security.Claims;
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
    IExternalSignInService? signInMethods = null,
    Infrastructure.Security.Mfa.MfaService? mfa = null) : ControllerBase
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

        // A person with an authenticator enrolled gets a challenge, not a session: the password
        // alone opens nothing. The challenge is single-use and short-lived (compliance AC-27).
        if (mfa is not null && await mfa.HasEnabledFactorAsync(user, ct))
        {
            var challenge = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            await redis.GetDatabase().StringSetAsync($"{MfaChallengePrefix}{challenge}", $"{user.TenantId:N}:{user.Id:N}", MfaChallengeTtl);
            return Ok(new { mfaRequired = true, challengeToken = challenge, expiresIn = (int)MfaChallengeTtl.TotalSeconds });
        }

        var issued = await Issuer.IssueAsync(user, ct);
        return Ok(new { accessToken = issued.AccessToken, refreshToken = issued.RefreshToken, tokenType = issued.TokenType, expiresIn = issued.ExpiresIn });
    }

    private const string MfaChallengePrefix = "mfa:challenge:";
    private static readonly TimeSpan MfaChallengeTtl = TimeSpan.FromMinutes(5);

    /// <summary>Second step of a local sign-in: the challenge from <c>login</c> plus an authenticator or backup code.</summary>
    [HttpPost("login/mfa")]
    public async Task<IActionResult> LoginMfa([FromBody] MfaLoginRequest request, CancellationToken ct)
    {
        if (mfa is null || string.IsNullOrWhiteSpace(request.ChallengeToken) || string.IsNullOrWhiteSpace(request.Code))
            return Unauthorized(new { error = "Invalid code." });

        var key = $"{MfaChallengePrefix}{request.ChallengeToken}";
        var redisDb = redis.GetDatabase();
        var subject = await redisDb.StringGetAsync(key);
        if (!subject.HasValue) return Unauthorized(new { error = "The sign-in challenge has expired. Sign in again." });

        var parts = subject.ToString().Split(':');
        var tenantId = Guid.ParseExact(parts[0], "N");
        var userId = Guid.ParseExact(parts[1], "N");
        var user = await Issuer.LoadForSignInAsync(tenantId, userId, ct);
        if (user is null || user.Status != AegisCore.Domain.UserStatus.Active) return Unauthorized(new { error = "Invalid code." });

        if (await bruteForce.IsLockedOutAsync(user.Email))
            return StatusCode(429, new { error = "Account temporarily locked due to too many failed attempts. Please try again in 15 minutes." });
        if (!await mfa.VerifyAsync(user, request.Code, ct))
        {
            await bruteForce.RecordFailedAttemptAsync(user.Email);
            return Unauthorized(new { error = "Invalid code." });
        }

        await redisDb.KeyDeleteAsync(key);
        await bruteForce.ClearFailedAttemptsAsync(user.Email);
        var issued = await Issuer.IssueAsync(user, ct, SessionTokenIssuer.AmrMfa);
        return Ok(new { accessToken = issued.AccessToken, refreshToken = issued.RefreshToken, tokenType = issued.TokenType, expiresIn = issued.ExpiresIn });
    }

    [Authorize]
    [HttpGet("mfa/status")]
    public async Task<IActionResult> MfaStatus(CancellationToken ct)
    {
        if (mfa is null) return NotFound();
        var user = await CurrentUserAsync(ct);
        if (user is null) return Unauthorized();
        return Ok(await mfa.StatusAsync(user, CurrentRoles(), RequiredRoles(), ct));
    }

    [Authorize]
    [HttpPost("mfa/enroll")]
    public async Task<IActionResult> MfaEnroll(CancellationToken ct)
    {
        if (mfa is null) return NotFound();
        var user = await CurrentUserAsync(ct);
        if (user is null) return Unauthorized();
        try
        {
            var enrolment = await mfa.EnrollAsync(user, ct);
            // The QR is drawn here, on the server: the otpauth URI carries the secret and must never
            // be handed to an external QR service.
            var png = new QRCoder.PngByteQRCode(new QRCoder.QRCodeGenerator().CreateQrCode(enrolment.OtpAuthUri, QRCoder.QRCodeGenerator.ECCLevel.M)).GetGraphic(5);
            return Ok(new { enrolment.Secret, enrolment.OtpAuthUri, enrolment.BackupCodes, qrPngDataUri = "data:image/png;base64," + Convert.ToBase64String(png) });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [Authorize]
    [HttpPost("mfa/confirm")]
    public async Task<IActionResult> MfaConfirm([FromBody] MfaCodeRequest request, CancellationToken ct)
    {
        if (mfa is null) return NotFound();
        var user = await CurrentUserAsync(ct);
        if (user is null) return Unauthorized();
        try
        {
            await mfa.ConfirmAsync(user, request.Code ?? string.Empty, ct);
            // The session that enrolled now counts as verified: no second sign-in to reach the rest of the portal.
            var issued = await Issuer.IssueAsync(user, ct, SessionTokenIssuer.AmrMfa);
            return Ok(new { enabled = true, accessToken = issued.AccessToken, refreshToken = issued.RefreshToken, tokenType = issued.TokenType, expiresIn = issued.ExpiresIn });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [Authorize]
    [HttpPost("mfa/disable")]
    public async Task<IActionResult> MfaDisable([FromBody] MfaCodeRequest request, CancellationToken ct)
    {
        if (mfa is null) return NotFound();
        var user = await CurrentUserAsync(ct);
        if (user is null) return Unauthorized();
        if (Infrastructure.Security.Mfa.MfaService.RoleRequiresMfa(CurrentRoles(), RequiredRoles()))
            return BadRequest(new { error = "An authenticator is required for your role and cannot be disabled." });
        try { await mfa.DisableAsync(user, request.Code ?? string.Empty, ct); return Ok(new { enabled = false }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    private async Task<AegisCore.Domain.Entities.User?> CurrentUserAsync(CancellationToken ct)
    {
        var tenantId = ResolveTenantId();
        return Guid.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var userId)
            ? await Issuer.LoadForSignInAsync(tenantId, userId, ct)
            : null;
    }

    private string[] CurrentRoles() => User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value)
        .Concat(User.FindAll("role").Select(c => c.Value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private string[]? RequiredRoles() => configuration.GetSection("Security:MfaRequiredRoles").Get<string[]>();

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

        // A refreshed session keeps how it was authenticated: a password-only session cannot become a verified one by refreshing.
        var amr = root.TryGetProperty("amr", out var amrElement) ? amrElement.GetString() ?? SessionTokenIssuer.AmrPassword : SessionTokenIssuer.AmrPassword;
        var issued = await issuer.IssueAsync(user, ct, amr);
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
    public sealed record MfaLoginRequest(string ChallengeToken, string Code);
    public sealed record MfaCodeRequest(string? Code);
    public sealed record RefreshRequest(string RefreshToken);
}
