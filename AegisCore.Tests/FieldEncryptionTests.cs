using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.FieldEncryption;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// Compliance control AC-20: identifiers, contact details, welfare text and sign-in locations are
/// ciphertext in the database, equality lookups on identifiers still work, and rows written before
/// encryption existed are read correctly and rewritten by the operator command.
/// </summary>
public sealed class FieldEncryptionTests
{
    private static AdmissionApplication Application(Guid tenant, string cnic) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenant, StudentName = "Sana Malik", Email = "sana@example.edu", Phone = "+92 300 1234567",
        Cnic = cnic, DateOfBirth = new DateOnly(2007, 3, 14), ProgramId = Guid.NewGuid(), QuotaType = "open_merit",
        ApplicationStatus = "Submitted", SubmittedAt = DateTimeOffset.UtcNow,
    };

    [Theory, DatabaseProviders]
    public async Task Cnic_and_phone_are_ciphertext_at_rest_and_the_cnic_is_still_searchable(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);

        var application = Application(tenant, "35202-1234567-1");
        db.AdmissionApplications.Add(application);
        await db.SaveChangesAsync();

        var stored = await db.Database.SqlQueryRaw<string>("select \"Cnic\" as \"Value\" from \"AdmissionApplications\"").SingleAsync();
        Assert.StartsWith(FieldCipher.DeterministicPrefix, stored);
        Assert.DoesNotContain("35202", stored);
        var storedPhone = await db.Database.SqlQueryRaw<string>("select \"Phone\" as \"Value\" from \"AdmissionApplications\"").SingleAsync();
        Assert.StartsWith(FieldCipher.RandomisedPrefix, storedPhone);

        db.ChangeTracker.Clear();
        var found = await db.AdmissionApplications.SingleOrDefaultAsync(a => a.Cnic == "35202-1234567-1");
        Assert.NotNull(found);
        Assert.Equal("+92 300 1234567", found.Phone);
        Assert.False(await db.AdmissionApplications.AnyAsync(a => a.Cnic == "35202-7654321-1"));
    }

    [Theory, DatabaseProviders]
    public async Task Rows_written_before_encryption_still_read_and_the_command_rewrites_them(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);

        // A legacy row: plaintext CNIC written straight to the table, as any pre-encryption install has.
        var legacy = Application(tenant, "placeholder");
        db.AdmissionApplications.Add(legacy);
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("update \"AdmissionApplications\" set \"Cnic\" = '42101-9999999-9', \"Phone\" = '0300-0000000'");
        db.ChangeTracker.Clear();

        var read = await db.AdmissionApplications.SingleAsync();
        Assert.Equal("42101-9999999-9", read.Cnic);
        Assert.Equal("0300-0000000", read.Phone);
        db.ChangeTracker.Clear();

        var rewritten = await FieldEncryptionMigrator.RewriteAsync(db, NullLogger.Instance);
        Assert.True(rewritten >= 1);

        var stored = await db.Database.SqlQueryRaw<string>("select \"Cnic\" as \"Value\" from \"AdmissionApplications\"").SingleAsync();
        Assert.StartsWith(FieldCipher.DeterministicPrefix, stored);
        Assert.NotNull(await db.AdmissionApplications.SingleOrDefaultAsync(a => a.Cnic == "42101-9999999-9"));

        // Running it again changes nothing: ciphertext is never re-encrypted.
        var again = await db.Database.SqlQueryRaw<string>("select \"Cnic\" as \"Value\" from \"AdmissionApplications\"").SingleAsync();
        await FieldEncryptionMigrator.RewriteAsync(db, NullLogger.Instance);
        Assert.Equal(again, await db.Database.SqlQueryRaw<string>("select \"Cnic\" as \"Value\" from \"AdmissionApplications\"").SingleAsync());
    }

    [Theory, DatabaseProviders]
    public async Task Sign_in_ip_and_coordinates_are_encrypted_and_round_trip(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);

        db.LoginAuditRecords.Add(new LoginAuditRecord
        {
            AuditId = Guid.NewGuid(), TenantId = tenant, UserEmail = "x@example.edu", UserRole = "Student", EventType = LoginEvent.Login,
            Provider = LoginProvider.Local, IpAddress = "198.51.100.7", IpCity = "Lahore", GpsLatitude = 31.5204, GpsLongitude = 74.3587,
            UserAgent = "Mozilla/5.0", IsSuccessful = true, OccurredAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var raw = await db.Database.SqlQueryRaw<string>("select \"GpsLatitude\" as \"Value\" from \"LoginAuditRecords\"").SingleAsync();
        Assert.StartsWith(FieldCipher.RandomisedPrefix, raw);
        var ip = await db.Database.SqlQueryRaw<string>("select \"IpAddress\" as \"Value\" from \"LoginAuditRecords\"").SingleAsync();
        Assert.StartsWith(FieldCipher.DeterministicPrefix, ip);

        db.ChangeTracker.Clear();
        var record = await db.LoginAuditRecords.SingleAsync(r => r.IpAddress == "198.51.100.7");
        Assert.Equal(31.5204, record.GpsLatitude);
        Assert.Equal(74.3587, record.GpsLongitude);
        Assert.Equal("Lahore", record.IpCity);
    }

    [Fact]
    public void Ciphertext_is_bound_to_its_column()
    {
        var protectedValue = FieldCipher.Protect("35202-1234567-1", "AdmissionApplication.Cnic", deterministic: true)!;
        Assert.Equal("35202-1234567-1", FieldCipher.Unprotect(protectedValue, "AdmissionApplication.Cnic"));
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => FieldCipher.Unprotect(protectedValue, "Visitor.CNIC"));
        Assert.Equal(protectedValue, FieldCipher.Protect(protectedValue, "AdmissionApplication.Cnic", deterministic: true));
    }
}
