using System.Security.Claims;

namespace AegisCore.API.Middleware;

/// <summary>
/// Supplies a deterministic administrator identity when Runtime:DemoMode is enabled.
/// Controlled by the AEGISCORE_DEMO_MODE environment variable.
/// </summary>
public sealed class DemoIdentityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IHostEnvironment environment, ILogger<DemoIdentityMiddleware> logger)
    {
        if (configuration.GetValue<bool>("Runtime:DemoMode"))
        {
            if (environment.IsProduction())
            {
                logger.LogCritical("Runtime:DemoMode is enabled in Production environment - refusing to inject demo identity");
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "configuration_error",
                    message = "Demo mode is not permitted in production environments."
                }, context.RequestAborted);
                return;
            }

            if (context.User.Identity?.IsAuthenticated != true)
            {
                var tenantId = configuration["TENANT_ID"]
                    ?? configuration["AEGISCORE_TENANT_ID"]
                    ?? "11111111-1111-1111-1111-111111111111";
                var userId = configuration["Runtime:DemoUserId"]
                    ?? "22222222-2222-2222-2222-222222222222";

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, "AegisCore Demo Administrator"),
                    new Claim(ClaimTypes.Email, "demo-admin@aegiscore.local"),
                    new Claim(ClaimTypes.Role, "SuperAdmin"),
                    new Claim(ClaimTypes.Role, "VC"),
                    new Claim("tenant_id", tenantId),
                    new Claim("permission", "*"),
                    new Claim("module", "*")
                };

                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "AegisCoreDemo"));
            }
        }

        await next(context);
    }
}
