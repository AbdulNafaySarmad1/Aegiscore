using AegisCore.Application.Licensing;
using Microsoft.AspNetCore.Authorization;

namespace AegisCore.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireModuleAttribute(string module) : AuthorizeAttribute($"module:{module}");

public sealed record ModuleRequirement(string Module) : IAuthorizationRequirement;

public sealed class ModuleAuthorizationHandler(ILicenseService licenses) : AuthorizationHandler<ModuleRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ModuleRequirement requirement)
    {
        if (await licenses.IsModuleEnabledAsync(requirement.Module))
        {
            context.Succeed(requirement);
        }
    }
}
