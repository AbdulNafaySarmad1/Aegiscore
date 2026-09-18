using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AegisCore.Tests;

/// <summary>
/// Production issues and validates RS256 tokens only. Nothing — not DemoSeed:Enabled, not a
/// configured HMAC secret — turns that off, a token names the key that signed it, and the
/// previous key keeps validating its tokens across a rotation.
/// </summary>
public sealed class JwtProductionKeyTests
{
    private static (string PrivatePem, string PublicPem) Pair()
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ExportRSAPrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    }

    private static IConfiguration Config(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value)).Build();

    [Theory]
    [InlineData("false")]
    [InlineData("true")]
    public void Production_Refuses_An_Hmac_Secret_Whatever_The_Demo_Seed_Flag_Says(string demoSeed)
    {
        var configuration = Config(("ASPNETCORE_ENVIRONMENT", "Production"), ("AEGISCORE_JWT_SECRET", new string('s', 64)), ("DemoSeed:Enabled", demoSeed));
        var problem = JwtKeyMaterial.ProductionKeyProblem(configuration);
        Assert.NotNull(problem);
        Assert.Contains("RSA only", problem);
        var ex = Assert.Throws<InvalidOperationException>(() => new JwtTokenService(configuration, Mock.Of<IConnectionMultiplexer>()));
        Assert.Contains("RSA only", ex.Message);
    }

    [Fact]
    public void Production_Names_The_Missing_Key_File()
    {
        var configuration = Config(("ASPNETCORE_ENVIRONMENT", "Production"),
            (JwtKeyMaterial.PrivateKeyPathKey, "/app/config/jwt-signing.key"), (JwtKeyMaterial.PublicKeyPathKey, "/app/config/jwt-signing.pub"));
        var problem = JwtKeyMaterial.ProductionKeyProblem(configuration)!;
        Assert.Contains("/app/config/jwt-signing.key", problem);
        Assert.Contains("restore it from backup", problem);
    }

    [Fact]
    public void Environment_Defaults_To_Production_When_Unset()
    {
        var configuration = Config(("AEGISCORE_JWT_SECRET", new string('s', 64)));
        Assert.True(JwtKeyMaterial.IsProduction(configuration));
        Assert.NotNull(JwtKeyMaterial.ProductionKeyProblem(configuration));
    }

    [Fact]
    public void Tokens_Are_RS256_And_Carry_The_Key_Id_And_A_Forged_HS256_Token_Is_Rejected()
    {
        var (privatePem, publicPem) = Pair();
        var secret = new string('s', 64);
        var configuration = Config(("ASPNETCORE_ENVIRONMENT", "Production"), (JwtKeyMaterial.PrivateKeyKey, privatePem), (JwtKeyMaterial.PublicKeyKey, publicPem),
            ("AEGISCORE_JWT_SECRET", secret)); // present but must be ignored
        var service = new JwtTokenService(configuration, Mock.Of<IConnectionMultiplexer>());
        var user = new User { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Email = "real@example.edu", Username = "real" };

        var token = service.GenerateAccessToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("RS256", parsed.Header.Alg);
        using var rsa = RSA.Create(); rsa.ImportFromPem(publicPem);
        Assert.Equal(JwtKeyMaterial.KeyId(rsa), parsed.Header.Kid);
        Assert.NotNull(service.ValidateToken(token));

        // The bypass path: an HS256 token signed with the configured secret, same issuer, audience and claims.
        var forged = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("https://aegiscore.pk", "aegiscore-api",
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("tenant_id", user.TenantId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256)));
        Assert.ThrowsAny<SecurityTokenException>(() => service.ValidateToken(forged));

        // And a token from a different RSA key is rejected too.
        var (otherPrivate, _) = Pair();
        using var other = RSA.Create(); other.ImportFromPem(otherPrivate);
        var foreign = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("https://aegiscore.pk", "aegiscore-api",
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new RsaSecurityKey(other), SecurityAlgorithms.RsaSha256)));
        Assert.ThrowsAny<SecurityTokenException>(() => service.ValidateToken(foreign));
    }

    [Fact]
    public void After_A_Rotation_The_Previous_Key_Still_Validates_The_Tokens_It_Signed()
    {
        var (oldPrivate, oldPublic) = Pair();
        var (newPrivate, newPublic) = Pair();
        var before = new JwtTokenService(Config(("ASPNETCORE_ENVIRONMENT", "Production"), (JwtKeyMaterial.PrivateKeyKey, oldPrivate), (JwtKeyMaterial.PublicKeyKey, oldPublic)), Mock.Of<IConnectionMultiplexer>());
        var user = new User { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Email = "r@example.edu", Username = "r" };
        var issuedBeforeRotation = before.GenerateAccessToken(user);

        var after = new JwtTokenService(Config(("ASPNETCORE_ENVIRONMENT", "Production"),
            (JwtKeyMaterial.PrivateKeyKey, newPrivate), (JwtKeyMaterial.PublicKeyKey, newPublic), (JwtKeyMaterial.PreviousPublicKeyKey, oldPublic)), Mock.Of<IConnectionMultiplexer>());
        Assert.NotNull(after.ValidateToken(issuedBeforeRotation));
        var issuedAfter = after.GenerateAccessToken(user);
        Assert.NotEqual(new JwtSecurityTokenHandler().ReadJwtToken(issuedBeforeRotation).Header.Kid, new JwtSecurityTokenHandler().ReadJwtToken(issuedAfter).Header.Kid);

        // Once the previous key is dropped, its tokens are dead.
        var dropped = new JwtTokenService(Config(("ASPNETCORE_ENVIRONMENT", "Production"), (JwtKeyMaterial.PrivateKeyKey, newPrivate), (JwtKeyMaterial.PublicKeyKey, newPublic)), Mock.Of<IConnectionMultiplexer>());
        Assert.ThrowsAny<SecurityTokenException>(() => dropped.ValidateToken(issuedBeforeRotation));
    }
}

public sealed class JwtKeyRotationTests
{
    [Fact]
    public void Rotate_Keeps_The_Previous_Public_Key_And_Writes_A_New_Pair()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aegis-rotate-" + Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            [JwtKeyMaterial.PrivateKeyPathKey] = Path.Combine(directory, "jwt-signing.key"),
            [JwtKeyMaterial.PublicKeyPathKey] = Path.Combine(directory, "jwt-signing.pub"),
        }).Build();
        try
        {
            Assert.True(JwtKeyMaterial.EnsureGenerated(configuration));
            var before = new JwtTokenService(configuration, Mock.Of<IConnectionMultiplexer>());
            var user = new User { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Email = "k@example.edu", Username = "k" };
            var token = before.GenerateAccessToken(user);
            var oldPublic = JwtKeyMaterial.PublicKeyPem(configuration);

            var message = JwtKeyMaterial.Rotate(configuration);
            Assert.Contains("Restart the application", message);
            Assert.Equal(Path.Combine(directory, "jwt-signing.previous.pub"), JwtKeyMaterial.PreviousPublicKeyPath(configuration));
            Assert.Equal(oldPublic, JwtKeyMaterial.PreviousPublicKeyPem(configuration));
            Assert.NotEqual(oldPublic, JwtKeyMaterial.PublicKeyPem(configuration));

            // The restarted service signs with the new key and still accepts the old token.
            var after = new JwtTokenService(configuration, Mock.Of<IConnectionMultiplexer>());
            Assert.NotNull(after.ValidateToken(token));
            Assert.Equal(2, JwtKeyMaterial.ValidationKeys(configuration).Count);
            Assert.NotEqual(new JwtSecurityTokenHandler().ReadJwtToken(token).Header.Kid, new JwtSecurityTokenHandler().ReadJwtToken(after.GenerateAccessToken(user)).Header.Kid);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
