using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using AegisCore.Domain.Entities;
using AegisCore.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Security;

/// <summary>
/// Service for generating and validating JWT tokens using RS256 when an RSA keypair
/// is configured and HS256 as a deployment-compatible fallback.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly SecurityKey _signingKey;
    private readonly SecurityKey _validationKey;
    private readonly string _algorithm;
    private readonly IConnectionMultiplexer _redis;
    private const string RefreshFamilyKeyPrefix = "aegiscore:auth:refresh:family:";
    private static readonly TimeSpan FamilyTtl = TimeSpan.FromDays(14);

    public JwtTokenService(IConfiguration configuration, IConnectionMultiplexer redis)
    {
        _redis = redis;
        var privateKeyPem = configuration["Jwt:PrivateKey"];
        var publicKeyPem = configuration["Jwt:PublicKey"];
        if (!string.IsNullOrWhiteSpace(privateKeyPem) && !privateKeyPem.StartsWith("${", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(publicKeyPem) && !publicKeyPem.StartsWith("${", StringComparison.Ordinal))
        {
            var privateKey = RSA.Create();
            privateKey.ImportFromPem(privateKeyPem.ToCharArray());
            var publicKey = RSA.Create();
            publicKey.ImportFromPem(publicKeyPem.ToCharArray());
            _signingKey = new RsaSecurityKey(privateKey);
            _validationKey = new RsaSecurityKey(publicKey);
            _algorithm = SecurityAlgorithms.RsaSha256;
            return;
        }

        var secret = configuration["AEGISCORE_JWT_SECRET"] ?? configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("JWT signing keys are not configured.");
        _signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        _validationKey = _signingKey;
        _algorithm = SecurityAlgorithms.HmacSha256;
    }

    public string GenerateAccessToken(User user)
    {
        var credentials = new SigningCredentials(_signingKey, _algorithm);
        var fullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName),
            new("tenant_id", user.TenantId.ToString())
        };

        var roleNames = user.UserRoles
            .Where(ur => ur.Role is not null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        claims.AddRange(roleNames.Select(role => new Claim(ClaimTypes.Role, role)));

        // In the current domain model student users intentionally share the same UUID
        // as their Student row. Emitting student_id makes the contract explicit and keeps
        // portal/API authorization independent from that implementation detail.
        if (roleNames.Contains("Student", StringComparer.Ordinal))
            claims.Add(new Claim("student_id", user.Id.ToString()));

        var token = new JwtSecurityToken(
            issuer: "https://aegiscore.pk",
            audience: "aegiscore-api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _validationKey,
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

    public async Task<bool> IsRefreshTokenFamilyValidAsync(Guid familyId)
    {
        var db = _redis.GetDatabase();
        var data = await db.StringGetAsync($"{RefreshFamilyKeyPrefix}{familyId}");
        if (!data.HasValue) return false;
        using var doc = JsonDocument.Parse(data.ToString());
        return doc.RootElement.GetProperty("valid").GetBoolean();
    }
}
