using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.Infrastructure.Security.Mfa;

namespace AegisCore.API.Middleware;

/// <summary>
/// A person whose role requires a second factor (<c>Security:MfaRequiredRoles</c>; Admin and
/// SuperAdmin by default) can reach only the sign-in, session and authenticator-enrolment
/// endpoints until their session was authenticated with one — or came from an identity provider,
/// which applies its own policy. Enforced per request so the rule holds for sessions that existed
/// before the role was granted. Compliance control AC-27.
/// </summary>
public sealed class MfaEnforcementMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPrefixes = ["/api/auth/", "/api/session", "/health", "/metrics", "/api/setup", "/api/wizard"];

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true && !configuration.GetValue<bool>("Runtime:DemoMode"))
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var allowed = AllowedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (!allowed)
            {
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).Concat(user.FindAll("role").Select(c => c.Value));
                var required = configuration.GetSection("Security:MfaRequiredRoles").Get<string[]>();
                var amr = user.FindFirstValue(SessionTokenIssuer.AmrClaim);
                if (MfaService.RoleRequiresMfa(roles, required) && amr is not (SessionTokenIssuer.AmrMfa or SessionTokenIssuer.AmrSso))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Your role requires an authenticator app. Set one up under Account → Security, then sign in again.",
                        code = "mfa_required"
                    });
                    return;
                }
            }
        }
        await next(context);
    }
}
