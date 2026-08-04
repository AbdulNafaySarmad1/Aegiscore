using AegisCore.Application.Hr;

namespace AegisCore.API.Finance;

public static class VisitingFacultyEndpoints
{
    public static IEndpointRouteBuilder MapVisitingFacultyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/hr/v2/faculty").RequireAuthorization().WithTags("HR");

        group.MapGet("/matches", async (HttpContext context, IVisitingFacultyService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.MatchFacultyToSlotsAsync(tenantId, ct));
        });

        group.MapPost("/vet/{profileId:guid}", async (Guid profileId, IVisitingFacultyService service, CancellationToken ct) =>
            Results.Ok(await service.VetFacultyProfileAsync(profileId, ct)));

        group.MapPost("/engage/{slotId:guid}/{profileId:guid}", async (Guid slotId, Guid profileId, IVisitingFacultyService service, CancellationToken ct) =>
            Results.Ok(await service.CreateEngagementAsync(slotId, profileId, ct)));

        group.MapPost("/rate/{engagementId:guid}", async (Guid engagementId, IVisitingFacultyService service, CancellationToken ct) =>
            Results.Ok(await service.RatePerformanceAsync(engagementId, ct)));

        group.MapPost("/compensate/{engagementId:guid}", async (Guid engagementId, IVisitingFacultyService service, CancellationToken ct) =>
            Results.Ok(await service.ProcessCompensationAsync(engagementId, ct)));

        group.MapGet("/pool-analytics", async (HttpContext context, IVisitingFacultyService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetPoolAnalyticsAsync(tenantId, ct));
        });

        return endpoints;
    }
}
