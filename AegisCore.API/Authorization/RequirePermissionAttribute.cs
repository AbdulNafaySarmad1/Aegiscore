using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AegisCore.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string resource, string action)
    : AuthorizeAttribute($"permission:{resource}:{action}");

public sealed record PermissionRequirement(string Resource, string Action) : IAuthorizationRequirement
{
    public PermissionRequirement(string module, string action, string resource) : this(resource, action) { }
}

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        const string permissionPrefix = "permission:";
        if (policyName.StartsWith(permissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var parts = policyName[permissionPrefix.Length..].Split(':', 2);
            if (parts.Length != 2)
            {
                return Task.FromResult<AuthorizationPolicy?>(null);
            }

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(parts[0], parts[1]))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}
