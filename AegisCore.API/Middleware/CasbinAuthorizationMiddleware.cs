using System.Security.Claims;
using Casbin;
using Microsoft.AspNetCore.Http;

namespace AegisCore.API.Middleware;

/// <summary>
/// Middleware that enforces Casbin ABAC authorization on every request.
/// </summary>
public class CasbinAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CasbinAuthorizationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public CasbinAuthorizationMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var roles = context.User?.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList() ?? [];

        if (roles.Contains("SuperAdmin"))
        {
            await _next(context);
            return;
        }

        var obj = context.Request.Path.Value ?? "/";
        var action = context.Request.Method;

        using var scope = _serviceProvider.CreateScope();
        var enforcer = scope.ServiceProvider.GetRequiredService<IEnforcer>();

        var allowed = false;

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            foreach (var role in roles)
            {
                if (await enforcer.EnforceAsync(role, obj, action))
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
                allowed = await enforcer.EnforceAsync("authenticated", obj, action);
        }
        else
        {
            allowed = await enforcer.EnforceAsync("anonymous", obj, action);
        }

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Forbidden" });
            return;
        }

        await _next(context);
    }
}
