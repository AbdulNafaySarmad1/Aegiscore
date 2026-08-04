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
    private readonly RSA _privateKey;
    private readonly RSA _publicKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public JwtTokenService(IConfiguration configuration)
    {
        var privateKeyPem = configuration["Jwt:PrivateKey"] ?? throw new InvalidOperationException("JWT private key not configured");
        var publicKeyPem = configuration["Jwt:PublicKey"] ?? throw new InvalidOperationException("JWT public key not configured");

        _privateKey = RSA.Create();
        _privateKey.ImportFromPem(privateKeyPem.ToCharArray());

        _publicKey = RSA.Create();
        _publicKey.ImportFromPem(publicKeyPem.ToCharArray());
    }

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        var credentials = new SigningCredentials(
            new RsaSecurityKey(_privateKey),
            SecurityAlgorithms.RsaSha256
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
            IssuerSigningKey = new RsaSecurityKey(_publicKey),
            ValidateIssuer = true,
            ValidIssuer = "https://aegiscore.pk",
            ValidateAudience = true,
            ValidAudience = "aegiscore-api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        return tokenHandler.ValidateToken(token, validationParameters, out _);
    }
}
