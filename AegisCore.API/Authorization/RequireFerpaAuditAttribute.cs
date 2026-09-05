using System.Security.Claims;
using AegisCore.Application.Compliance;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AegisCore.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireFerpaAuditAttribute(string resourceType, string studentIdParam) : Attribute, IAsyncActionFilter
{
    public string ResourceType { get; } = resourceType;
    public string StudentIdParam { get; } = studentIdParam;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var identifier = ResolveIdentifier(context, StudentIdParam);
        var userId = Guid.TryParse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;
        var roles = context.HttpContext.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value)
            .Concat(context.HttpContext.User.FindAll("role").Select(claim => claim.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        var action = path.Contains("export", StringComparison.OrdinalIgnoreCase) ? "Export"
            : path.Contains("print", StringComparison.OrdinalIgnoreCase) ? "Print"
            : "Read";
        var request = new FerpaAccessRequest(
            userId,
            roles,
            context.HttpContext.Connection.RemoteIpAddress?.ToString(),
            ResourceType,
            StudentIdParam,
            identifier,
            action);
        var audit = context.HttpContext.RequestServices.GetRequiredService<IFerpaAuditService>();
        var decision = await audit.AuthorizeAndAuditAsync(request, context.HttpContext.RequestAborted);
        if (!decision.IsJustified)
        {
            context.Result = new ObjectResult(new
            {
                error = "ferpa_access_denied",
                message = "You do not have authorisation to access this student's records."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }

    private static Guid ResolveIdentifier(ActionExecutingContext context, string parameterName)
    {
        if (context.ActionArguments.TryGetValue(parameterName, out var argument))
        {
            if (argument is Guid guid) return guid;
            if (Guid.TryParse(argument?.ToString(), out var parsed)) return parsed;
        }

        if (context.RouteData.Values.TryGetValue(parameterName, out var routeValue)
            && Guid.TryParse(routeValue?.ToString(), out var routeGuid))
            return routeGuid;
        if (context.HttpContext.Request.Query.TryGetValue(parameterName, out var queryValue)
            && Guid.TryParse(queryValue.ToString(), out var queryGuid))
            return queryGuid;
        return Guid.Empty;
    }
}
