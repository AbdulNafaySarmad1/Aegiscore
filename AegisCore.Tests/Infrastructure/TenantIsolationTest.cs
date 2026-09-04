using AegisCore.Domain.Common;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests.Infrastructure;

public class TenantIsolationTest
{
    [Fact]
    public void AllDbSets_MustImplement_ITenantScoped()
    {
        var dbSetTypes = typeof(AegisDbContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                   p.PropertyType.GetGenericTypeDefinition()
                   == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .ToList();

        var failures = dbSetTypes
            .Where(t => !typeof(ITenantScoped).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        Assert.True(
            failures.Count == 0,
            $"The following entities are not ITenantScoped and " +
            $"will leak cross-tenant data: " +
            $"{string.Join(", ", failures)}. " +
            $"Add ITenantScoped before merging."
        );
    }
}
