using System.Text.Json;
using AegisCore.Domain.Common;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests.Infrastructure;

public sealed class TenantIsolationTest
{
    [Fact]
    public void All_DbSet_entities_must_be_tenant_scoped()
    {
        var entityTypes = typeof(AegisDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Distinct()
            .OrderBy(t => t.FullName)
            .ToArray();

        var compliant = entityTypes
            .Where(t => typeof(ITenantScoped).IsAssignableFrom(t))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(x => x)
            .ToArray();

        var missingInterface = entityTypes
            .Where(t => !typeof(ITenantScoped).IsAssignableFrom(t))
            .ToArray();

        var list1 = missingInterface
            .Where(t => t.GetProperty(nameof(ITenantScoped.TenantId))?.PropertyType == typeof(Guid))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(x => x)
            .ToArray();

        var list2 = missingInterface
            .Where(t => t.GetProperty(nameof(ITenantScoped.TenantId))?.PropertyType != typeof(Guid))
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(x => x)
            .ToArray();

        var reportDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
        Directory.CreateDirectory(reportDirectory);
        File.WriteAllText(
            Path.Combine(reportDirectory, "tenant-isolation-report.json"),
            JsonSerializer.Serialize(new
            {
                TotalDbSetEntities = entityTypes.Length,
                AlreadyCompliant = compliant,
                List1HasTenantIdNoInterface = list1,
                List2NoTenantId = list2
            }, new JsonSerializerOptions { WriteIndented = true }));

        Assert.True(
            missingInterface.Length == 0,
            $"Tenant isolation gaps detected. LIST1_HAS_TENANTID_NO_INTERFACE ({list1.Length}): {string.Join(", ", list1)}{Environment.NewLine}" +
            $"LIST2_NO_TENANTID ({list2.Length}): {string.Join(", ", list2)}");
    }
}
