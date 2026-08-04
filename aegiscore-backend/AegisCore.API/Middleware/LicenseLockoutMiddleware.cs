using AegisCore.Application.Licensing;

namespace AegisCore.API.Middleware;

public sealed class LicenseLockoutMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health")
            || path.StartsWithSegments("/api/license")
            || path.StartsWithSegments("/api/admin/license"))
        {
            await next(context);
            return;
        }

        var status = await licenseService.GetStatusAsync(context.RequestAborted);
        if (!status.IsLocked)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "System License Invalid",
            reason = status.LockReason ?? status.Status,
            locked_at = status.LockedAt,
            contact = "Please contact support"
        }, context.RequestAborted);
    }
}
