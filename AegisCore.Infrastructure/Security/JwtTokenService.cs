using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using AegisCore.Domain.Entities;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Security;

/// <summary>
/// Service for generating and validating JWT tokens using RS256.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly SecurityKey _signingKey;
    private readonly IReadOnlyList<SecurityKey> _validationKeys;
    private readonly string _algorithm;
    private readonly IConnectionMultiplexer _redis;
    private readonly int _sessionLifetimeSeconds;
    private const string RefreshFamilyKeyPrefix = "aegiscore:auth:refresh:family:";
    private static readonly TimeSpan FamilyTtl = TimeSpan.FromDays(14);

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="redis">The Redis connection multiplexer.</param>
    public JwtTokenService(IConfiguration configuration, IConnectionMultiplexer redis)
    {
        _redis = redis;
        _sessionLifetimeSeconds = int.TryParse(configuration["SESSION_LIFETIME"], out var lifetime) && lifetime is >= 60 and <= 86_400
            ? lifetime
            : 900;
        var problem = JwtKeyMaterial.ProductionKeyProblem(configuration);
        if (problem is not null) throw new InvalidOperationException(problem);
        var signingKey = JwtKeyMaterial.SigningKey(configuration);
        var validationKeys = JwtKeyMaterial.ValidationKeys(configuration);
        if (signingKey is not null && validationKeys.Count > 0)
        {
            _signingKey = signingKey;
            _validationKeys = validationKeys;
            _algorithm = SecurityAlgorithms.RsaSha256;
            return;
        }

        // Development only; production was refused above.
        var secret = configuration["AEGISCORE_JWT_SECRET"] ?? configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("JWT signing keys are not configured: set Jwt:PrivateKeyPath/PublicKeyPath (RSA) or, in Development, AEGISCORE_JWT_SECRET.");
        _signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        _validationKeys = [_signingKey];
        _algorithm = SecurityAlgorithms.HmacSha256;
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user, IReadOnlyDictionary<string, string>? extraClaims = null)
    {
        var credentials = new SigningCredentials(
            _signingKey,
            _algorithm
        );

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("tenant_id", user.TenantId.ToString())
        };
        // The person's own name, so that every screen and every audit line attributes work to a
        // person rather than to an email address. Absent when an account was created without one,
        // and the session endpoint falls back to the email then rather than inventing a name.
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName)) claims.Add(new Claim(ClaimTypes.Name, fullName));
        claims.AddRange(user.UserRoles.Select(ur => new Claim(ClaimTypes.Role, ur.Role.Name)));
        // student_id and department_id are read by the session endpoint and by every student-facing
        // screen, and nothing ever issued them: every student was told their account was not linked
        // to a student record, which made the student half of the portal unreachable.
        if (extraClaims is not null)
            foreach (var claim in extraClaims)
                if (!string.IsNullOrWhiteSpace(claim.Value)) claims.Add(new Claim(claim.Key, claim.Value));

        var token = new JwtSecurityToken(
            issuer: "https://aegiscore.pk",
            audience: "aegiscore-api",
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_sessionLifetimeSeconds),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = _validationKeys,
            ValidateIssuer = true,
            ValidIssuer = "https://aegiscore.pk",
            ValidateAudience = true,
            ValidAudience = "aegiscore-api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [_algorithm]
        };

        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }

    /// <inheritdoc />
    public async Task<Guid> GenerateRefreshTokenFamilyAsync(Guid userId)
    {
        var familyId = Guid.NewGuid();
        var db = _redis.GetDatabase();
        var data = JsonSerializer.Serialize(new
        {
            valid = true,
            user_id = userId.ToString(),
            created_at = DateTime.UtcNow.ToString("O")
        });
        await db.StringSetAsync($"{RefreshFamilyKeyPrefix}{familyId}", data, FamilyTtl);
        return familyId;
    }

    /// <inheritdoc />
    public async Task InvalidateRefreshTokenFamilyAsync(Guid familyId)
    {
        var db = _redis.GetDatabase();
        var key = $"{RefreshFamilyKeyPrefix}{familyId}";
        var existing = await db.StringGetAsync(key);
        if (!existing.HasValue) return;
        using var doc = JsonDocument.Parse(existing.ToString());
        var root = doc.RootElement;
        var updated = new
        {
            valid = false,
            user_id = root.GetProperty("user_id").GetString()!,
            created_at = root.GetProperty("created_at").GetString()!
        };
        await db.StringSetAsync(key, JsonSerializer.Serialize(updated), FamilyTtl);
    }

    /// <inheritdoc />
    public async Task<bool> IsRefreshTokenFamilyValidAsync(Guid familyId)
    {
        var db = _redis.GetDatabase();
        var data = await db.StringGetAsync($"{RefreshFamilyKeyPrefix}{familyId}");
        if (!data.HasValue) return false;
        using var doc = JsonDocument.Parse(data.ToString());
        return doc.RootElement.GetProperty("valid").GetBoolean();
    }
}
