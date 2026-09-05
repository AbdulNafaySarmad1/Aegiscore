using System.Security.Claims;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly AegisDbContext _db;
    private readonly ICasbinPolicySyncService? _casbin;

    public PermissionAuthorizationHandler(AegisDbContext db, ICasbinPolicySyncService? casbin = null)
    {
        _db = db;
        _casbin = casbin;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userIdText = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdText, out var userId))
        {
            return;
        }

        var tenantIdText = context.User.FindFirstValue("tenant_id");
        var roles = context.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (_casbin is not null && Guid.TryParse(tenantIdText, out var tenantId) && roles.Length > 0)
        {
            if (await _casbin.EnforceAsync(tenantId, roles, requirement.Resource, requirement.Action, CancellationToken.None))
                context.Succeed(requirement);
            return;
        }

        // Compatibility path for isolated handler tests; production authorization always uses Casbin.
        var allowed = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId && (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
            .SelectMany(ur => ur.Role.RolePermissions)
            .AnyAsync(rp =>
                rp.Permission.Action == requirement.Action &&
                rp.Permission.Resource == requirement.Resource);

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}
