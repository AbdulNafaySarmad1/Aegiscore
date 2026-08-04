using System.Security.Claims;

namespace AegisCore.API.Middleware;

public sealed class TenantContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirstValue("tenant_id")
                ?? context.User.FindFirstValue("tid")
                ?? context.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");
            if (!Guid.TryParse(claim, out var tenantId) || tenantId == Guid.Empty)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "tenant_context_required",
                    message = "The authenticated identity does not contain a valid tenant claim."
                }, context.RequestAborted);
                return;
            }

            context.Items["TenantId"] = tenantId;
        }

        await next(context);
    }
}
