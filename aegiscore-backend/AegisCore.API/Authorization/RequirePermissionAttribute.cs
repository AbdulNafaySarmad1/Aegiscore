using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AegisCore.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string module, string action, string resource)
    : AuthorizeAttribute($"permission:{module}:{action}:{resource}");

public sealed record PermissionRequirement(string Module, string Action, string Resource) : IAuthorizationRequirement;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        const string permissionPrefix = "permission:";
        if (policyName.StartsWith(permissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var parts = policyName[permissionPrefix.Length..].Split(':', 3);
            if (parts.Length != 3)
            {
                return Task.FromResult<AuthorizationPolicy?>(null);
            }

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(parts[0], parts[1], parts[2]))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        const string modulePrefix = "module:";
        if (policyName.StartsWith(modulePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ModuleRequirement(policyName[modulePrefix.Length..]))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}
