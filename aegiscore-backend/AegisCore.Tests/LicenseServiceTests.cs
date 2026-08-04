using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Licensing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests;

public sealed class LicenseServiceTests
{
    [Fact]
    public async Task ValidateAsync_Accepts_Signed_Unexpired_License()
    {
        var service = CreateService();
        var license = SignedLicense(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));

        var result = await service.ValidateAsync(license);

        Assert.True(result.IsValid);
        Assert.Equal("SSUET", result.License?.UniversityName);
    }

    [Fact]
    public async Task ValidateAsync_Rejects_Expired_License()
    {
        var service = CreateService();
        var license = SignedLicense(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));

        var result = await service.ValidateAsync(license);

        Assert.False(result.IsValid);
        Assert.Equal(AegisCore.Application.Licensing.LicenseValidationFailureReason.Expired, result.FailureReason);
    }

    [Fact]
    public async Task ValidateAsync_Rejects_Invalid_Signature()
    {
        var service = CreateService();
        var license = SignedLicense(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))).Replace("SSUET", "OTHER");

        var result = await service.ValidateAsync(license);

        Assert.False(result.IsValid);
        Assert.Equal(AegisCore.Application.Licensing.LicenseValidationFailureReason.InvalidSignature, result.FailureReason);
    }

    [Fact]
    public async Task ValidateAsync_Rejects_Malformed_Signed_Payload_Without_Throwing()
    {
        var service = CreateService();
        var license = SignedLicense("not-a-date");

        var result = await service.ValidateAsync(license);

        Assert.False(result.IsValid);
        Assert.Equal(AegisCore.Application.Licensing.LicenseValidationFailureReason.InvalidSignature, result.FailureReason);
    }

    [Fact]
    public async Task GetStatusAsync_ReLocks_After_Override_Expires()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options;
        await using var db = new AegisDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.LicenseStates.Add(new LicenseState
        {
            LicenseKey = "redacted",
            UniversityId = Guid.NewGuid(),
            UniversityName = "SSUET",
            LicenseJson = "{}",
            FailedHeartbeatCount = 2,
            OverrideUntil = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["License:HmacSecret"] = "test-secret-with-enough-entropy"
        }).Build();
        var service = new LicenseService(db, config, null!, new AegisCore.Infrastructure.Foundation.AuditService(db), NullLogger<LicenseService>.Instance);

        var status = await service.GetStatusAsync();

        Assert.True(status.IsLocked);
        Assert.Equal("heartbeat_failed", status.LockReason);
    }

    private static LicenseService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["License:HmacSecret"] = "test-secret-with-enough-entropy" })
            .Build();

        return new LicenseService(null!, config, null!, null!, NullLogger<LicenseService>.Instance);
    }

    private static string SignedLicense(DateOnly expiry)
        => SignedLicense(expiry.ToString("yyyy-MM-dd"));

    private static string SignedLicense(string expiry)
    {
        var payload = new JsonObject
        {
            ["university_id"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            ["university_name"] = "SSUET",
            ["issued_date"] = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            ["expiry_date"] = expiry,
            ["max_concurrent_users"] = 1000,
            ["licensed_modules"] = new JsonArray("academic_calendar", "admissions"),
            ["issued_by"] = "apex-systems"
        };

        var bytes = Encoding.UTF8.GetBytes(payload.ToJsonString());
        var key = Encoding.UTF8.GetBytes("test-secret-with-enough-entropy");
        payload["signature"] = Convert.ToHexString(HMACSHA256.HashData(key, bytes)).ToLowerInvariant();
        return payload.ToJsonString();
    }
}
