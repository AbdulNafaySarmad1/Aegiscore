using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace AegisCore.API.Authorization;

public sealed class PermissionAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallback = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        var permission = authorizeResult.AuthorizationFailure?.FailedRequirements.OfType<PermissionRequirement>().FirstOrDefault();
        if (authorizeResult.Forbidden && permission is not null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = $"You do not have {permission.Action} permission on {permission.Resource}."
            });
            return;
        }

        await _fallback.HandleAsync(next, context, policy, authorizeResult);
    }
}
