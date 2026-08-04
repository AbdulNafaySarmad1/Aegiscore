using AegisCore.Infrastructure.Installation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.API.Middleware;

public class SetupRequiredMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IInstallationStateService _installationState;

    public SetupRequiredMiddleware(RequestDelegate next, IInstallationStateService installationState)
    {
        _next = next;
        _installationState = installationState;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var isInstalled = _installationState.IsInstalled();

        var isSetupPath = path.StartsWith("/setup", StringComparison.OrdinalIgnoreCase) ||
                          path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                          path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                          path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
                          path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
                          path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                          path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                          path == "/";

        if (!isInstalled && !isSetupPath)
        {
            context.Response.Redirect("/setup/");
            return;
        }

        await _next(context);
    }
}

public static class SetupRequiredMiddlewareExtensions
{
    public static IApplicationBuilder UseSetupRequired(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SetupRequiredMiddleware>();
    }
}