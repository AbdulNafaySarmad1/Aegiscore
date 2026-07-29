using System.Security.Claims;

namespace AegisCore.API.Middleware;

/// <summary>
/// Supplies a deterministic administrator identity only for the explicitly enabled
/// local demo runtime. Production startup rejects demo mode in Program.cs.
/// </summary>
public sealed class DemoIdentityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction()
            && configuration.GetValue<bool>("Runtime:DemoMode")
            && context.User.Identity?.IsAuthenticated != true)
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

        await next(context);
    }
}
