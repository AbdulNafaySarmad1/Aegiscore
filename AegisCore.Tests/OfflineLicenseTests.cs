using System.Security.Cryptography;
using AegisCore.Application.Licensing;
using AegisCore.Infrastructure.Licensing;
using AegisCore.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

/// <summary>
/// An offline licence is verified with the vendor's public key and nothing else: no authority,
/// no shared secret, no demo flag. It is bound to one university and expires on its date.
/// </summary>
public sealed class OfflineLicenseTests
{
    private static (string Private, string Public) IssuerPair()
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    }

    private static readonly DateOnly Today = new(2026, 9, 18);

    [Fact]
    public void Issued_Key_Verifies_With_The_Issuer_Public_Key_Only()
    {
        var (priv, pub) = IssuerPair();
        var (_, otherPub) = IssuerPair();
        var university = Guid.NewGuid();
        var key = OfflineLicense.Issue(priv, new LicenseInfo(university, "Fictional National University", Today, Today.AddYears(1), 500, "AegisCore"), "Premium");
        Assert.StartsWith("AEGIS-OFFLINE.", key);

        var ok = OfflineLicense.Validate(key, pub, Today);
        Assert.True(ok.IsValid, ok.ValidationMessage);
        Assert.Equal(university, ok.License!.UniversityId);
        Assert.Equal("Premium", OfflineLicense.Tier(key));
        Assert.Contains("valid until", ok.ValidationMessage);

        var wrongKey = OfflineLicense.Validate(key, otherPub, Today);
        Assert.False(wrongKey.IsValid);
        Assert.Contains("not issued for this product", wrongKey.ValidationMessage);

        // Any change to the payload breaks the signature.
        var payload = OfflineLicense.PayloadJson(key).Replace("\"max_concurrent_users\":500", "\"max_concurrent_users\":5000");
        var forged = "AEGIS-OFFLINE." + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_') + "." + key.Split('.')[2];
        Assert.False(OfflineLicense.Validate(forged, pub, Today).IsValid);
        Assert.Contains("malformed", OfflineLicense.Validate("AEGIS-OFFLINE.not-a-key", pub, Today).ValidationMessage);
    }

    [Fact]
    public void Key_Is_Time_Boxed()
    {
        var (priv, pub) = IssuerPair();
        var info = new LicenseInfo(Guid.NewGuid(), "U", Today, Today.AddDays(30), 100, "AegisCore");
        var key = OfflineLicense.Issue(priv, info);
        Assert.True(OfflineLicense.Validate(key, pub, Today.AddDays(30)).IsValid);
        var expired = OfflineLicense.Validate(key, pub, Today.AddDays(31));
        Assert.False(expired.IsValid);
        Assert.Equal(LicenseValidationFailureReason.Expired, expired.FailureReason);
        Assert.Contains("expired on", expired.ValidationMessage);
        Assert.False(OfflineLicense.Validate(key, pub, Today.AddDays(-1)).IsValid); // not before issue
    }

    [Fact]
    public async Task Activation_And_Heartbeat_Need_No_Network_And_No_Demo_Flag()
    {
        var (priv, pub) = IssuerPair();
        var tenant = Guid.NewGuid();
        var university = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres: false);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var key = OfflineLicense.Issue(priv, new LicenseInfo(university, "Fictional National University", DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1), 500, "AegisCore"), "Premium");

        // No HTTP client at all: any attempt to reach an authority would throw.
        var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TENANT_ID"] = tenant.ToString(),
            [OfflineLicense.IssuerPublicKeyKey] = pub,
            ["DemoSeed:Enabled"] = "false",
        }).Build();
        var audit = new Mock<AegisCore.Application.Foundation.IAuditService>();
        var service = new LicenseService(db, configuration, factory.Object, audit.Object, NullLogger<LicenseService>.Instance);

        await service.ActivateAsync(key, university, CancellationToken.None);
        var heartbeat = await service.SendHeartbeatAsync(CancellationToken.None);
        Assert.True(heartbeat.Success, heartbeat.Message);
        Assert.Equal("offline", heartbeat.Status);

        var status = await service.GetStatusAsync(CancellationToken.None);
        Assert.False(status.IsLocked);
        Assert.Equal("active", status.Status);
        Assert.Equal(LicenseTier.Premium, status.Tier);
        Assert.Equal(0, status.FailedHeartbeatCount);

        // The university binding holds.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ActivateAsync(key, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Demo_Seed_Flag_No_Longer_Passes_The_Heartbeat()
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres: false);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        db.LicenseStates.Add(new AegisCore.Domain.Entities.LicenseState
        {
            Id = Guid.NewGuid(), TenantId = tenant, UniversityId = Guid.NewGuid(), UniversityName = "U", LicenseKey = "{}", LicenseJson = "{}", Status = "active", CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TENANT_ID"] = tenant.ToString(), ["DemoSeed:Enabled"] = "true", // the old bypass
        }).Build();
        var service = new LicenseService(db, configuration, factory.Object, Mock.Of<AegisCore.Application.Foundation.IAuditService>(), NullLogger<LicenseService>.Instance);

        var heartbeat = await service.SendHeartbeatAsync(CancellationToken.None);
        Assert.False(heartbeat.Success);
        Assert.NotEqual("demo-local", heartbeat.Status);
    }
}
