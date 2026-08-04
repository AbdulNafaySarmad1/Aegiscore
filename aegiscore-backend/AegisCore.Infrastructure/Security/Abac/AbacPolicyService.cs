using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AegisCore.Infrastructure.Security.Abac;

public interface IAbacPolicyService
{
    Task<bool> AuthorizeAsync(HttpContext context, CancellationToken cancellationToken = default);
}

public class AbacPolicyService : IAbacPolicyService
{
    private readonly ILogger<AbacPolicyService> _logger;

    public AbacPolicyService(ILogger<AbacPolicyService> logger)
    {
        _logger = logger;
    }

    public Task<bool> AuthorizeAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var user = context.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
            return Task.FromResult(true);

        var tenantId = user.FindFirst("tenant_id")?.Value
            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
        var routeTenant = context.Request.Headers["X-Tenant-Id"].ToString();

        if (!string.IsNullOrEmpty(routeTenant) && !string.IsNullOrEmpty(tenantId)
            && !string.Equals(routeTenant, tenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("ABAC tenant mismatch for {User}.", user.Identity.Name);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}

public class AbacMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AbacMiddleware> _logger;

    public AbacMiddleware(RequestDelegate next, ILogger<AbacMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var policy = context.RequestServices.GetRequiredService<IAbacPolicyService>();
        if (!await policy.AuthorizeAsync(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "abac_denied", message = "Attribute-based access policy denied this request." }));
            return;
        }

        await _next(context);
    }
}

public static class AbacServiceExtensions
{
    public static IServiceCollection AddAbacAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAbacPolicyService, AbacPolicyService>();
        return services;
    }
}
