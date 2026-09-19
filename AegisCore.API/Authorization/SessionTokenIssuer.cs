using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AegisCore.API.Authorization;

/// <summary>The tokens a sign-in ends with, whichever door the person came through.</summary>
public sealed record IssuedTokens(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);

/// <summary>
/// The tail of every sign-in — password, refresh, or an identity provider's callback: Casbin sync
/// for the tenant, the person's own claims (student and department), the RS256 access token, a
/// fresh refresh-token family in Redis, LastLoginAt. One function, so a token issued after an SSO
/// sign-in is indistinguishable to every downstream check from one issued after a password login.
/// </summary>
public sealed class SessionTokenIssuer(
    AegisDbContext db,
    IJwtTokenService tokens,
    IConnectionMultiplexer redis,
    IConfiguration configuration,
    ICasbinPolicySyncService? casbin = null)
{
    public const string RefreshTokenKeyPrefix = "aegiscore:auth:refresh:token:";
    private static readonly TimeSpan RefreshTokenTtl = TimeSpan.FromDays(7);

    /// <summary>Loads the person with roles for token issuance; sign-in has no ambient tenant, so the tenant is explicit.</summary>
    public Task<User?> LoadForSignInAsync(Guid tenantId, Guid userId, CancellationToken ct)
        => db.Users.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == userId, ct);

    public async Task<IssuedTokens> IssueAsync(User user, CancellationToken ct)
    {
        if (casbin is not null) await casbin.SyncTenantAsync(user.TenantId, ct);
        user.LastLoginAt = DateTime.UtcNow;

        var accessToken = tokens.GenerateAccessToken(user, await IdentityClaimsAsync(user, ct));
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

        return new IssuedTokens(accessToken, refreshToken, "Bearer", SessionLifetimeSeconds());
    }

    /// <summary>
    /// What this account is, beyond its name: a student's own record id, and the department they
    /// belong to. A student account is created with the same id as its student record (see the
    /// admissions enrolment step), so the link needs no column — but the claim has to be issued, and
    /// until it was, /api/session answered "studentId: null" for every student alive.
    /// </summary>
    public async Task<Dictionary<string, string>> IdentityClaimsAsync(User user, CancellationToken ct)
    {
        var claims = new Dictionary<string, string>(StringComparer.Ordinal);
        // Sign-in has no ambient tenant yet, so the tenant filter would hide every row: the user's
        // own tenant is the scope here.
        var student = await db.Students.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.Id == user.Id && s.TenantId == user.TenantId)
            .Select(s => new { s.Id, s.BatchId })
            .SingleOrDefaultAsync(ct);
        if (student is not null)
        {
            claims["student_id"] = student.Id.ToString();
            var department = await db.Batches.IgnoreQueryFilters().AsNoTracking()
                .Where(b => b.Id == student.BatchId && b.TenantId == user.TenantId)
                .Select(b => (Guid?)b.DepartmentId)
                .SingleOrDefaultAsync(ct);
            if (department is { } departmentId && departmentId != Guid.Empty) claims["department_id"] = departmentId.ToString();
            return claims;
        }

        var faculty = await db.Faculties.IgnoreQueryFilters().AsNoTracking()
            .Where(f => f.Id == user.Id && f.TenantId == user.TenantId)
            .Select(f => (Guid?)f.DepartmentId)
            .SingleOrDefaultAsync(ct);
        if (faculty is { } facultyDepartment && facultyDepartment != Guid.Empty) claims["department_id"] = facultyDepartment.ToString();
        return claims;
    }

    public int SessionLifetimeSeconds()
        => int.TryParse(configuration["SESSION_LIFETIME"], out var seconds) && seconds is >= 60 and <= 86_400 ? seconds : 900;
}
