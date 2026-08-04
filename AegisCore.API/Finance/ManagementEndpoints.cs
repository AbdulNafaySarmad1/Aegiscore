using AegisCore.Application.Governance;

namespace AegisCore.API.Finance;

public static class ManagementEndpoints
{
    public static IEndpointRouteBuilder MapAccreditationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/management/v2/accreditation").RequireAuthorization().WithTags("Management");

        group.MapGet("/framework/{frameworkId:guid}", async (Guid frameworkId, IAccreditationService service, CancellationToken ct) =>
            Results.Ok(await service.ComputeFrameworkScoreAsync(frameworkId, ct)));

        group.MapGet("/frameworks", async (HttpContext context, IAccreditationService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetAllFrameworkScoresAsync(tenantId, ct));
        });

        group.MapGet("/gaps/{frameworkId:guid}", async (Guid frameworkId, IAccreditationService service, CancellationToken ct) =>
            Results.Ok(await service.GetGapAnalysisAsync(frameworkId, ct)));

        group.MapGet("/findings/{frameworkId:guid}", async (Guid frameworkId, IAccreditationService service, CancellationToken ct) =>
            Results.Ok(await service.GetPriorFindingsTrackerAsync(frameworkId, ct)));

        group.MapGet("/predictive/{frameworkId:guid}", async (Guid frameworkId, IAccreditationService service, CancellationToken ct) =>
            Results.Ok(await service.GetPredictiveReadinessAsync(frameworkId, ct)));

        group.MapGet("/stale-evidence", async (HttpContext context, IAccreditationService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.CheckEvidenceStalenessAsync(tenantId, ct));
        });

        return endpoints;
    }

    public static IEndpointRouteBuilder MapBoardPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/management/v2/board").RequireAuthorization().WithTags("Management");

        group.MapGet("/dashboard", async (HttpContext context, IBoardPortalService service, CancellationToken ct) =>
        {
            var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "board_member";
            return Results.Ok(await service.GetExecutiveDashboardAsync(role, ct));
        });

        group.MapGet("/pack/{meetingId:guid}", async (Guid meetingId, IBoardPortalService service, CancellationToken ct) =>
            Results.Ok(await service.GenerateBoardPackAsync(meetingId, ct)));

        group.MapGet("/strategic-plan", async (HttpContext context, IBoardPortalService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetStrategicPlanStatusAsync(tenantId, ct));
        });

        group.MapGet("/benchmarks", async (HttpContext context, IBoardPortalService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetBenchmarkComparisonAsync(tenantId, ct));
        });

        group.MapGet("/alerts", async (HttpContext context, IBoardPortalService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetBoardAlertsAsync(tenantId, ct));
        });

        group.MapGet("/decisions", async (HttpContext context, IBoardPortalService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetPendingDecisionsAsync(tenantId, ct));
        });

        return endpoints;
    }
}
