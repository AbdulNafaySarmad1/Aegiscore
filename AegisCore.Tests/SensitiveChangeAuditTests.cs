using System.Security.Claims;
using System.Text.Json;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security;
using AegisCore.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// Compliance control AC-51: a change to a sensitive record leaves an audit row naming the actor,
/// the record, the fields and — for non-identifying fields — the old and new values, whichever
/// code path made the change. Identity and secret values are recorded as changed, never copied.
/// </summary>
public sealed class SensitiveChangeAuditTests
{
    private static readonly Guid Actor = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static HttpContext HttpFor(Guid tenant)
    {
        var http = new DefaultHttpContext();
        http.Items["TenantId"] = tenant;
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Actor.ToString()),
            new Claim(ClaimTypes.Email, "controller@example.edu"),
            new Claim(ClaimTypes.Role, "Exam Controller"),
        ], "test"));
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.1.2.3");
        return http;
    }

    [Theory, DatabaseProviders]
    public async Task A_salary_change_is_audited_with_actor_old_and_new_values(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(HttpFor(tenant));

        var employee = new Employee { Id = Guid.NewGuid(), TenantId = tenant, FirstName = "Bilal", LastName = "Ahmed", DepartmentId = Guid.NewGuid(), Salary = 100_000m };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        employee.Salary = 125_000m;
        await db.SaveChangesAsync();

        var rows = await db.AuditLogs.Where(a => a.ResourceId == employee.Id).OrderBy(a => a.Timestamp).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal("hr.Employee.created", rows[0].Action);
        Assert.Equal("hr.Employee.updated", rows[1].Action);
        Assert.All(rows, r => { Assert.Equal(Actor, r.UserId); Assert.Equal(tenant, r.TenantId); Assert.Equal("10.1.2.3", r.IpAddress); });

        using var details = JsonDocument.Parse(rows[1].Details!);
        Assert.Equal("controller@example.edu", details.RootElement.GetProperty("actorEmail").GetString());
        var change = details.RootElement.GetProperty("changes").EnumerateArray().Single();
        Assert.Equal("Salary", change.GetProperty("name").GetString());
        Assert.StartsWith("100000", change.GetProperty("old").GetString());
        Assert.StartsWith("125000", change.GetProperty("new").GetString());
    }

    [Theory, DatabaseProviders]
    public async Task Names_and_password_hashes_are_recorded_as_changed_but_never_copied(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(HttpFor(tenant));

        var user = new User { Id = Guid.NewGuid(), TenantId = tenant, Username = "ayesha", Email = "ayesha@example.edu", FirstName = "Ayesha", LastName = "Khan", PasswordHash = "$argon2id$old", Status = UserStatus.Active };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        user.LastName = "Raza";
        user.PasswordHash = "$argon2id$new";
        await db.SaveChangesAsync();

        var updated = await db.AuditLogs.SingleAsync(a => a.ResourceId == user.Id && a.Action == "identity.User.updated");
        Assert.DoesNotContain("Khan", updated.Details);
        Assert.DoesNotContain("Raza", updated.Details);
        Assert.DoesNotContain("argon2", updated.Details);
        using var details = JsonDocument.Parse(updated.Details!);
        var names = details.RootElement.GetProperty("changes").EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToHashSet();
        Assert.Contains("LastName", names);
        Assert.Contains("PasswordHash", names);
        Assert.All(details.RootElement.GetProperty("changes").EnumerateArray(), c => Assert.True(c.GetProperty("redacted").GetBoolean()));
    }

    [Theory, DatabaseProviders]
    public async Task Unlisted_entities_and_no_op_updates_write_nothing(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(HttpFor(tenant));

        db.TranslationKeys.Add(new TranslationKey { Id = Guid.NewGuid(), TenantId = tenant, Key = "k", Module = "m", CreatedAt = DateTime.UtcNow });
        var employee = new Employee { Id = Guid.NewGuid(), TenantId = tenant, FirstName = "Bilal", LastName = "Ahmed", DepartmentId = Guid.NewGuid(), Salary = 100_000m };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        var before = await db.AuditLogs.CountAsync();

        employee.Salary = 100_000m; // same value: nothing to say
        await db.SaveChangesAsync();

        Assert.Equal(1, before);
        Assert.Equal(before, await db.AuditLogs.CountAsync());
        Assert.False(SensitiveChangeAuditInterceptor.AuditedEntities.ContainsKey(typeof(TranslationKey)));
    }
}
