using AegisCore.Application.Compliance;
using AegisCore.Infrastructure.Compliance;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AegisCore.Tests;

/// <summary>Compliance control AC-50: a read of a staff, payroll or welfare record is logged with an encrypted IP.</summary>
public sealed class AccessAuditTests
{
    [Theory, DatabaseProviders]
    public async Task A_read_of_an_employee_record_is_logged_and_the_ip_is_not_stored_in_clear(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(v => (byte)v).ToArray());
        var crypto = new PqcCryptographyService(Options.Create(new PqcCryptographyOptions { MasterKeyBase64 = key }), NullLogger<PqcCryptographyService>.Instance);
        var service = new AccessAuditService(db, crypto);
        var reader = Guid.NewGuid();
        var employee = Guid.NewGuid();

        await service.RecordAsync(new AccessAuditRecord(reader, ["HR"], "203.0.113.9", "Employee", employee, "Read"));

        var row = await db.FerpaAuditLogs.SingleAsync(l => l.ResourceId == employee);
        Assert.Equal(reader, row.UserId);
        Assert.Equal("HR", row.UserRole);
        Assert.Equal("Employee", row.ResourceType);
        Assert.Equal("Read", row.Action);
        Assert.True(row.Justified);
        Assert.Equal(Guid.Empty, row.StudentId);
        Assert.NotNull(row.IpAddressEncrypted);
        Assert.DoesNotContain("203.0.113.9", Convert.ToHexString(row.IpAddressEncrypted!));
    }
}
