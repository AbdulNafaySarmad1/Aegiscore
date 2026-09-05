using AegisCore.Application.Onboarding;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardRolePermissionService(AegisDbContext db, WizardStateService wizardState)
{
    private static readonly HashSet<string> AllowedResources = new(StringComparer.OrdinalIgnoreCase)
    {
        "Students", "Grades", "Attendance", "Finance", "Timetable", "Admissions", "Reports", "Settings", "System"
    };

    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Read", "Write", "Delete", "Approve", "Export"
    };

    public async Task<IReadOnlyList<Role>> SaveRolesAsync(string token, SaveWizardRolesRequest request, CancellationToken ct = default)
    {
        var tenantId = await TenantIdAsync(token, ct);
        var names = request.Roles.Select(name => name?.Trim() ?? string.Empty).ToArray();
        if (names.Length == 0 || names.Any(name => name.Length is 0 or > 64))
            throw new ArgumentException("At least one role is required and every role name must contain 1 to 64 characters.");
        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
            throw new ArgumentException("Role names must be unique.");

        var existing = await db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == tenantId).ToListAsync(ct);
        var retainedIds = new HashSet<Guid>();
        foreach (var name in names)
        {
            var role = existing.SingleOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (role is null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, Name = name,
                    Description = "Created by the setup wizard.", IsSystem = false
                };
                db.Roles.Add(role);
            }
            else
            {
                role.Name = name;
            }
            retainedIds.Add(role.Id);
        }

        var removed = existing.Where(role => !role.IsSystem && !retainedIds.Contains(role.Id)).ToArray();
        if (removed.Length > 0)
        {
            var removedIds = removed.Select(role => role.Id).ToArray();
            var grants = await db.RolePermissions.IgnoreQueryFilters().Where(grant => grant.TenantId == tenantId && removedIds.Contains(grant.RoleId)).ToListAsync(ct);
            db.RolePermissions.RemoveRange(grants);
            db.Roles.RemoveRange(removed);
        }

        await db.SaveChangesAsync(ct);
        return await db.Roles.IgnoreQueryFilters().AsNoTracking().Where(role => role.TenantId == tenantId).OrderBy(role => role.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Permission>> SavePermissionsAsync(string token, SaveWizardPermissionDefinitionsRequest request, CancellationToken ct = default)
    {
        var tenantId = await TenantIdAsync(token, ct);
        var definitions = request.Permissions
            .Select(item => new WizardPermissionDefinition(NormalizeResource(item.Resource), NormalizeAction(item.Action)))
            .DistinctBy(item => $"{item.Resource}:{item.Action}", StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (definitions.Length == 0) throw new ArgumentException("At least one permission is required.");

        var existing = await db.Permissions.IgnoreQueryFilters().Where(permission => permission.TenantId == tenantId).ToListAsync(ct);
        foreach (var definition in definitions)
        {
            var permission = existing.SingleOrDefault(item =>
                item.Resource.Equals(definition.Resource, StringComparison.OrdinalIgnoreCase) &&
                item.Action.Equals(definition.Action, StringComparison.OrdinalIgnoreCase));
            if (permission is null)
            {
                permission = new Permission
                {
                    Id = Guid.NewGuid(), TenantId = tenantId,
                    Name = $"{definition.Resource}.{definition.Action}",
                    Resource = definition.Resource, Action = definition.Action,
                    Module = definition.Resource, Description = "Created by the setup wizard."
                };
                db.Permissions.Add(permission);
            }
        }

        await db.SaveChangesAsync(ct);
        return await db.Permissions.IgnoreQueryFilters().AsNoTracking().Where(permission => permission.TenantId == tenantId)
            .OrderBy(permission => permission.Resource).ThenBy(permission => permission.Action).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RolePermission>> SaveMatrixAsync(string token, SaveWizardPermissionMatrixRequest request, CancellationToken ct = default)
    {
        var tenantId = await TenantIdAsync(token, ct);
        if (request.Entries.Count > 10_000) throw new ArgumentException("The permission matrix cannot exceed 10,000 entries.");

        var entries = request.Entries.Select(item => new WizardRolePermissionSelection(
            NormalizeRole(item.Role), NormalizeResource(item.Resource), NormalizeAction(item.Action), item.Allowed)).ToArray();
        var roles = await db.Roles.IgnoreQueryFilters().Where(role => role.TenantId == tenantId).ToListAsync(ct);
        var permissions = await db.Permissions.IgnoreQueryFilters().Where(permission => permission.TenantId == tenantId).ToListAsync(ct);
        foreach (var entry in entries)
        {
            if (!roles.Any(role => role.Name.Equals(entry.Role, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Role '{entry.Role}' does not exist.");
            if (!permissions.Any(permission => permission.Resource.Equals(entry.Resource, StringComparison.OrdinalIgnoreCase) && permission.Action.Equals(entry.Action, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"Permission '{entry.Resource}.{entry.Action}' does not exist.");
        }

        var roleIds = roles.Select(role => role.Id).ToArray();
        var oldGrants = await db.RolePermissions.IgnoreQueryFilters()
            .Where(grant => grant.TenantId == tenantId && roleIds.Contains(grant.RoleId)).ToListAsync(ct);
        db.RolePermissions.RemoveRange(oldGrants);
        foreach (var entry in entries.Where(item => item.Allowed)
                     .DistinctBy(item => $"{item.Role}:{item.Resource}:{item.Action}", StringComparer.OrdinalIgnoreCase))
        {
            var role = roles.Single(item => item.Name.Equals(entry.Role, StringComparison.OrdinalIgnoreCase));
            var permission = permissions.Single(item => item.Resource.Equals(entry.Resource, StringComparison.OrdinalIgnoreCase) && item.Action.Equals(entry.Action, StringComparison.OrdinalIgnoreCase));
            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(), TenantId = tenantId, RoleId = role.Id, PermissionId = permission.Id
            });
        }

        await db.SaveChangesAsync(ct);
        return await db.RolePermissions.IgnoreQueryFilters().AsNoTracking()
            .Where(grant => grant.TenantId == tenantId).ToListAsync(ct);
    }

    private async Task<Guid> TenantIdAsync(string token, CancellationToken ct)
    {
        var state = await wizardState.ResumeWizardAsync(token, ct);
        if (state.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before configuring permissions.");
        return state.TenantId;
    }

    private static string NormalizeRole(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length is > 0 and <= 64 ? normalized : throw new ArgumentException("Role names must contain 1 to 64 characters.");
    }

    private static string NormalizeResource(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return AllowedResources.FirstOrDefault(item => item.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown permission resource '{normalized}'.");
    }

    private static string NormalizeAction(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return AllowedActions.FirstOrDefault(item => item.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown permission action '{normalized}'.");
    }
}
