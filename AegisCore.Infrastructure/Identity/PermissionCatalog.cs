using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Identity;

/// <summary>
/// The permission resources the API is gated on, grouped by the category of personal data each
/// one opens (docs/compliance/01-data-inventory.md). Staff, payroll and welfare records used to
/// sit under the generic <c>System</c> resource, so anyone with an operations permission could
/// read salaries and counselling queues; they now have their own resources (control AC-29).
/// </summary>
public static class PermissionCatalog
{
    public static readonly string[] Resources =
    [
        "Students", "Grades", "Attendance", "CampusPolygon", "Finance", "Timetable", "Admissions", "Reports", "Settings", "System",
        "Staff", "Payroll", "Welfare",
        // Gated on by the convocation endpoints since before this catalogue existed, but absent from
        // the wizard's list, so no role could ever be granted it.
        "Graduation",
    ];

    public static readonly string[] Actions = ["Read", "Write", "Delete", "Approve", "Export", "Admin"];

    /// <summary>Resources added after the first release; granted at startup to every role that already holds the administrator marker.</summary>
    public static readonly string[] AddedResources = ["Staff", "Payroll", "Welfare", "Graduation"];

    /// <summary>The grant that marks a role as an administrator in the wizard and the seeders.</summary>
    public const string AdministratorMarkerResource = "System";
    public const string AdministratorMarkerAction = "Write";

    /// <summary>
    /// An installation upgraded from a release where HR, payroll and welfare lived under
    /// <c>System</c> would otherwise lock its administrators out of those screens. Every role holding
    /// <c>System.Write</c> receives the new resources with every action. Idempotent; runs at startup.
    /// </summary>
    public static async Task<int> GrantAddedResourcesToAdministratorsAsync(AegisDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var adminRoles = await db.RolePermissions.IgnoreQueryFilters().AsNoTracking()
            .Where(grant => grant.Permission.Resource == AdministratorMarkerResource && grant.Permission.Action == AdministratorMarkerAction)
            .Select(grant => new { grant.TenantId, grant.RoleId })
            .Distinct()
            .ToListAsync(ct);
        if (adminRoles.Count == 0) return 0;

        var tenantIds = adminRoles.Select(r => r.TenantId).Distinct().ToList();
        var permissions = await db.Permissions.IgnoreQueryFilters()
            .Where(p => tenantIds.Contains(p.TenantId) && AddedResources.Contains(p.Resource))
            .ToListAsync(ct);
        var grants = await db.RolePermissions.IgnoreQueryFilters().AsNoTracking()
            .Where(g => tenantIds.Contains(g.TenantId) && AddedResources.Contains(g.Permission.Resource))
            .Select(g => new { g.RoleId, g.PermissionId })
            .ToListAsync(ct);
        var granted = grants.Select(g => (g.RoleId, g.PermissionId)).ToHashSet();

        var added = 0;
        foreach (var tenantId in tenantIds)
        {
            foreach (var resource in AddedResources)
            {
                foreach (var action in Actions)
                {
                    var permission = permissions.SingleOrDefault(p => p.TenantId == tenantId && p.Resource == resource && p.Action == action);
                    if (permission is null)
                    {
                        permission = new Permission
                        {
                            Id = Guid.NewGuid(), TenantId = tenantId, Name = $"{resource}.{action}", Resource = resource, Action = action,
                            Module = resource, Description = "Added by the permission catalogue upgrade.",
                        };
                        db.Permissions.Add(permission);
                        permissions.Add(permission);
                    }
                    foreach (var role in adminRoles.Where(r => r.TenantId == tenantId))
                    {
                        if (granted.Contains((role.RoleId, permission.Id))) continue;
                        db.RolePermissions.Add(new RolePermission { Id = Guid.NewGuid(), TenantId = tenantId, RoleId = role.RoleId, PermissionId = permission.Id });
                        granted.Add((role.RoleId, permission.Id));
                        added++;
                    }
                }
            }
        }
        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Permission catalogue upgrade: {Count} grant(s) added for {Resources} to administrator roles.", added, string.Join(", ", AddedResources));
        }
        return added;
    }
}
