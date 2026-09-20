using System.Security.Claims;
using AegisCore.Application.Compliance;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AegisCore.API.Authorization;

/// <summary>
/// Logs who read a sensitive non-student record (staff, payroll, welfare, health, foreign-student
/// status) — the FERPA-style attribute enforces a student relationship and cannot be used on these.
/// Authorisation is unchanged: whatever the permission layer decided stands; this only records it.
/// Applied as <c>[RequireAccessAudit("Employee", "id")]</c>; a missing identifier logs a list read.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireAccessAuditAttribute(string resourceType, string? identifierParam = null) : Attribute, IAsyncActionFilter
{
    public string ResourceType { get; } = resourceType;
    public string? IdentifierParam { get; } = identifierParam;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var userId = Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : Guid.Empty;
        var roles = http.User.FindAll(ClaimTypes.Role).Select(c => c.Value)
            .Concat(http.User.FindAll("role").Select(c => c.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var path = http.Request.Path.Value ?? string.Empty;
        var action = path.Contains("export", StringComparison.OrdinalIgnoreCase) ? "Export"
            : path.Contains("print", StringComparison.OrdinalIgnoreCase) ? "Print"
            : IdentifierParam is null ? "List" : "Read";
        var identifier = IdentifierParam is null ? Guid.Empty : Resolve(context, IdentifierParam);

        var audit = http.RequestServices.GetRequiredService<IAccessAuditService>();
        await audit.RecordAsync(new AccessAuditRecord(userId, roles, http.Connection.RemoteIpAddress?.ToString(), ResourceType, identifier, action), http.RequestAborted);
        await next();
    }

    private static Guid Resolve(ActionExecutingContext context, string name)
    {
        if (context.ActionArguments.TryGetValue(name, out var argument))
        {
            if (argument is Guid guid) return guid;
            if (Guid.TryParse(argument?.ToString(), out var fromArgument)) return fromArgument;
        }
        if (context.RouteData.Values.TryGetValue(name, out var route) && Guid.TryParse(route?.ToString(), out var fromRoute)) return fromRoute;
        if (context.HttpContext.Request.Query.TryGetValue(name, out var query) && Guid.TryParse(query.ToString(), out var fromQuery)) return fromQuery;
        return Guid.Empty;
    }
}
