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
        var subject = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var obj = context.Request.Path.Value ?? "/";
        var action = context.Request.Method;

        using var scope = _serviceProvider.CreateScope();
        var enforcer = scope.ServiceProvider.GetRequiredService<IEnforcer>();

        var allowed = await enforcer.EnforceAsync(subject, obj, action);

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
