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

        // The same sentence the write interceptor uses, so the person sees one explanation wherever the lock bites.
        var why = status.LockReason switch
        {
            "heartbeat_failed" => "the licence authority could not be reached for two consecutive heartbeats",
            "expired" => "the licence has expired",
            "revoked" => "the licence was revoked by the vendor",
            "invalid" => "the licence key is not valid",
            "pending_activation" => "no licence has been activated yet",
            _ => "the licence is locked (" + (status.LockReason ?? status.Status) + ")"
        };
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            error = $"This installation is read-only: {why}. Reading and exporting records still work. An administrator can restore write access on the licence page (/admin/license) by entering an offline licence key from the vendor or by reconnecting to the licence authority.",
            code = "license_read_only",
            readOnly = true,
            reason = status.LockReason ?? status.Status,
            lockedAt = status.LockedAt,
            licensePage = "/admin/license"
        }, context.RequestAborted);
    }

    private static readonly string[] PremiumModules = ["/api/ai", "/api/proctoring", "/api/plagiarism"];
}
