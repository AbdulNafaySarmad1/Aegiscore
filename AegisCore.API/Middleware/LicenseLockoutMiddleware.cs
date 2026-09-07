using AegisCore.Application.Licensing;

namespace AegisCore.API.Middleware;

public sealed class LicenseLockoutMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ILicenseService licenseService,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (configuration.GetValue<bool>("Runtime:DemoMode"))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path;
        if (path.StartsWithSegments("/health")
            || path.StartsWithSegments("/api/auth")
            || path.StartsWithSegments("/api/license")
            || path.StartsWithSegments("/api/admin/license"))
        {
            await next(context);
            return;
        }

        var status = await licenseService.GetStatusAsync(context.RequestAborted);
        var read = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method);
        if (status.Tier != LicenseTier.Premium && PremiumModules.Any(module => path.StartsWithSegments(module)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Premium license required", tier = status.Tier.ToString() }, context.RequestAborted);
            return;
        }
        if (!status.IsLocked || read)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "System License Invalid",
            readOnly = true,
            reason = status.LockReason ?? status.Status,
            locked_at = status.LockedAt,
            contact = "Please contact support"
        }, context.RequestAborted);
    }

    private static readonly string[] PremiumModules = ["/api/ai", "/api/proctoring", "/api/plagiarism"];
}
