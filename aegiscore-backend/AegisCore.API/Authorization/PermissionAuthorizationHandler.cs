using System.Security.Claims;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Authorization;

public sealed class PermissionAuthorizationHandler(AegisDbContext db) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole("SuperAdmin") || context.User.IsInRole("ViceChancellor"))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdText = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdText, out var userId))
        {
            return;
        }

        var allowed = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId && (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
            .SelectMany(ur => ur.Role.RolePermissions)
            .AnyAsync(rp =>
                rp.Permission.Module == requirement.Module &&
                rp.Permission.Action == requirement.Action &&
                rp.Permission.Resource == requirement.Resource);

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}
