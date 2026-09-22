using System.Reflection;
using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Identity;
using AegisCore.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// Compliance control AC-29 (docs/compliance/04-nist-csf-gap-assessment.md): roles are checked
/// against the categories of personal data in the inventory, not against features. Staff, payroll
/// and welfare records used to sit behind the generic <c>System</c> permission, so an operations
/// role could read salaries and counselling queues; these tests hold the split in place.
/// </summary>
public sealed class PermissionMatrixTests
{
    /// <summary>Data category → the permission resource that opens it (docs/compliance/01-data-inventory.md §2).</summary>
    private static readonly (string Category, string Resource, string Action)[] Categories =
    [
        ("Student identity and academic record", "Students", "Read"),
        ("Marks and grades", "Grades", "Read"),
        ("Attendance incl. coordinates", "Attendance", "Read"),
        ("Applicant records incl. CNIC and date of birth", "Admissions", "Read"),
        ("Fees and payments", "Finance", "Read"),
        ("Staff records incl. salary", "Staff", "Read"),
        ("Payroll and bank references", "Payroll", "Read"),
        ("Counselling, discipline, mental health, health records", "Welfare", "Read"),
        ("Installation settings and secrets", "System", "Write"),
    ];

    /// <summary>What each role legitimately needs. Anything not listed must be refused.</summary>
    private static readonly Dictionary<string, string[]> Expected = new()
    {
        ["Admin"] = ["Students", "Grades", "Attendance", "Admissions", "Finance", "Staff", "Payroll", "Welfare", "System"],
        ["Registrar"] = ["Students", "Grades", "Attendance", "Admissions"],
        ["Faculty"] = ["Students", "Grades", "Attendance"],
        ["Student"] = ["Grades", "Attendance"],
        ["HR"] = ["Staff", "Payroll"],
        ["Counsellor"] = ["Students", "Welfare"],
        ["CFO"] = ["Finance"],
        ["IT"] = ["System"],
    };

    private static readonly Dictionary<string, string[]> Grants = new()
    {
        ["Admin"] = ["Students.Read", "Grades.Read", "Attendance.Read", "Admissions.Read", "Finance.Read", "Staff.Read", "Payroll.Read", "Welfare.Read", "System.Write"],
        ["Registrar"] = ["Students.Read", "Grades.Read", "Attendance.Read", "Admissions.Read"],
        ["Faculty"] = ["Students.Read", "Grades.Read", "Attendance.Read"],
        ["Student"] = ["Grades.Read", "Attendance.Read"],
        ["HR"] = ["Staff.Read", "Payroll.Read"],
        ["Counsellor"] = ["Students.Read", "Welfare.Read"],
        ["CFO"] = ["Finance.Read"],
        ["IT"] = ["System.Write"],
    };

    [Theory, DatabaseProviders]
    public async Task Each_role_reaches_its_own_data_categories_and_no_others(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);

        var users = new Dictionary<string, Guid>();
        foreach (var (roleName, grants) in Grants)
        {
            var role = new Role { Id = Guid.NewGuid(), TenantId = tenant, Name = roleName, Description = roleName };
            var user = new User { Id = Guid.NewGuid(), TenantId = tenant, Username = roleName, Email = $"{roleName}@example.edu", FirstName = roleName, LastName = "Test", Status = UserStatus.Active };
            db.AddRange(role, user);
            db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), TenantId = tenant, UserId = user.Id, RoleId = role.Id });
            foreach (var grant in grants)
            {
                var parts = grant.Split('.');
                var permission = await db.Permissions.IgnoreQueryFilters().SingleOrDefaultAsync(p => p.TenantId == tenant && p.Resource == parts[0] && p.Action == parts[1]);
                if (permission is null)
                {
                    permission = new Permission { Id = Guid.NewGuid(), TenantId = tenant, Name = grant, Resource = parts[0], Action = parts[1], Module = parts[0] };
                    db.Permissions.Add(permission);
                    await db.SaveChangesAsync();
                }
                db.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), TenantId = tenant, RoleId = role.Id, PermissionId = permission.Id });
            }
            users[roleName] = user.Id;
            await db.SaveChangesAsync();
        }

        var handler = new PermissionAuthorizationHandler(db);
        var failures = new List<string>();
        foreach (var (roleName, userId) in users)
        {
            foreach (var (category, resource, action) in Categories)
            {
                var principal = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, roleName)], "test"));
                var requirement = new PermissionRequirement(resource, action);
                var context = new AuthorizationHandlerContext([requirement], principal, null);
                await handler.HandleAsync(context);

                var shouldReach = Expected[roleName].Contains(resource);
                if (context.HasSucceeded != shouldReach)
                    failures.Add($"{roleName} → {category} ({resource}.{action}): expected {(shouldReach ? "allowed" : "refused")}, got {(context.HasSucceeded ? "allowed" : "refused")}");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void Staff_payroll_and_welfare_endpoints_are_not_gated_on_the_generic_system_permission()
    {
        var controllers = typeof(AegisCore.API.Controllers.HrController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);
        var offenders = new List<string>();

        foreach (var controller in controllers)
        {
            var route = controller.GetCustomAttributes<RouteAttribute>().FirstOrDefault()?.Template ?? string.Empty;
            if (!route.Contains("hr", StringComparison.OrdinalIgnoreCase)
                && !route.Contains("mental-health", StringComparison.OrdinalIgnoreCase)) continue;

            var policies = controller.GetCustomAttributes<RequirePermissionAttribute>().Select(a => a.Policy)
                .Concat(controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .SelectMany(m => m.GetCustomAttributes<RequirePermissionAttribute>()).Select(a => a.Policy));
            offenders.AddRange(policies.Where(p => p is not null && p.StartsWith("permission:System:", StringComparison.Ordinal))
                .Select(p => $"{controller.Name}: {p}"));
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void The_catalogue_lists_every_resource_the_api_is_gated_on()
    {
        var used = typeof(AegisCore.API.Controllers.HrController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetCustomAttributes<RequirePermissionAttribute>()
                .Concat(t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .SelectMany(m => m.GetCustomAttributes<RequirePermissionAttribute>())))
            .Select(a => a.Policy!.Split(':')[1])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var missing = used.Except(PermissionCatalog.Resources, StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.Empty(missing);
    }

    [Theory, DatabaseProviders]
    public async Task An_upgraded_installation_gives_its_administrators_the_new_resources(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);

        // The state before this release: one administrator role holding System.Write, nothing else.
        var role = new Role { Id = Guid.NewGuid(), TenantId = tenant, Name = "Admin", Description = "Admin" };
        var permission = new Permission { Id = Guid.NewGuid(), TenantId = tenant, Name = "System.Write", Resource = "System", Action = "Write", Module = "System" };
        db.AddRange(role, permission);
        db.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), TenantId = tenant, RoleId = role.Id, PermissionId = permission.Id });
        var faculty = new Role { Id = Guid.NewGuid(), TenantId = tenant, Name = "Faculty", Description = "Faculty" };
        db.Roles.Add(faculty);
        await db.SaveChangesAsync();

        var added = await PermissionCatalog.GrantAddedResourcesToAdministratorsAsync(db, NullLogger.Instance);
        Assert.Equal(PermissionCatalog.AddedResources.Length * PermissionCatalog.Actions.Length, added);

        var adminResources = await db.RolePermissions.IgnoreQueryFilters()
            .Where(g => g.RoleId == role.Id).Select(g => g.Permission.Resource).Distinct().ToListAsync();
        Assert.Contains("Staff", adminResources);
        Assert.Contains("Payroll", adminResources);
        Assert.Contains("Welfare", adminResources);
        Assert.Empty(await db.RolePermissions.IgnoreQueryFilters().Where(g => g.RoleId == faculty.Id).ToListAsync());

        // Running it again grants nothing further.
        Assert.Equal(0, await PermissionCatalog.GrantAddedResourcesToAdministratorsAsync(db, NullLogger.Instance));
    }
}
