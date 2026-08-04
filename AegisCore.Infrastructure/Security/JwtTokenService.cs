using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using AegisCore.Domain.Entities;
using AegisCore.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Security;

/// <summary>
/// Service for generating and validating JWT tokens using RS256.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly SecurityKey _signingKey;
    private readonly SecurityKey _validationKey;
    private readonly string _algorithm;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public JwtTokenService(IConfiguration configuration)
    {
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

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
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
        claims.AddRange(user.UserRoles.Select(ur => new Claim(ClaimTypes.Role, ur.Role.Name)));

        var token = new JwtSecurityToken(
            issuer: "https://aegiscore.pk",
            audience: "aegiscore-api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
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
}
