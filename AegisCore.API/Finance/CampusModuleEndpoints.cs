using AegisCore.Application.Campus;
using AegisCore.Application.CampusOperations;

namespace AegisCore.API.Finance;

public static class CampusModuleEndpoints
{
    public static IEndpointRouteBuilder MapElectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/campus/v2/elections").RequireAuthorization().WithTags("CampusOperations");

        group.MapPost("/", async (ElectionSetupRequest request, IElectionService service, CancellationToken ct) =>
            Results.Ok(await service.CreateElectionAsync(request, ct)));

        group.MapGet("/{electionId:guid}/candidates", async (Guid electionId, IElectionService service, CancellationToken ct) =>
            Results.Ok(await service.GetVerifiedCandidatesAsync(electionId, ct)));

        group.MapPost("/{electionId:guid}/token/{studentId:guid}", async (Guid electionId, Guid studentId, IElectionService service, CancellationToken ct) =>
            Results.Ok(await service.IssueVoterTokenAsync(electionId, studentId, ct)));

        group.MapPost("/{electionId:guid}/ballot", async (Guid electionId, CastBallotRequest request, IElectionService service, CancellationToken ct) =>
            Results.Ok(await service.CastBallotAsync(request, ct)));

        group.MapGet("/{electionId:guid}/results", async (Guid electionId, IElectionService service, CancellationToken ct) =>
            Results.Ok(await service.ComputeResultsAsync(electionId, ct)));

        group.MapGet("/{electionId:guid}/participation", async (Guid electionId, IElectionService service, CancellationToken ct) =>
            Results.Ok(await service.GetParticipationStatsAsync(electionId, ct)));

        return endpoints;
    }

    public static IEndpointRouteBuilder MapCafeteriaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/campus/v2/cafeteria").RequireAuthorization().WithTags("Campus");

        group.MapGet("/menu/{unitId:guid}", async (Guid unitId, ICafeteriaService service, CancellationToken ct) =>
            Results.Ok(await service.GetDailyMenuAsync(unitId, ct)));

        group.MapPost("/order", async (PreOrderRequest request, ICafeteriaService service, CancellationToken ct) =>
            Results.Ok(await service.PlacePreOrderAsync(request, ct)));

        group.MapPost("/pay", async (CafeteriaPaymentRequest request, ICafeteriaService service, CancellationToken ct) =>
            Results.Ok(await service.ProcessPaymentAsync(request, ct)));

        group.MapGet("/forecast/{unitId:guid}", async (Guid unitId, ICafeteriaService service, CancellationToken ct) =>
            Results.Ok(await service.GetDemandForecastAsync(unitId, ct)));

        group.MapGet("/vendor/{vendorId:guid}/performance", async (Guid vendorId, ICafeteriaService service, CancellationToken ct) =>
            Results.Ok(await service.GetVendorPerformanceAsync(vendorId, ct)));

        group.MapGet("/analytics", async (HttpContext context, ICafeteriaService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetAnalyticsAsync(tenantId, ct));
        });

        group.MapGet("/inventory/{unitId:guid}/alerts", async (Guid unitId, ICafeteriaService service, CancellationToken ct) =>
            Results.Ok(await service.GetInventoryAlertsAsync(unitId, ct)));

        return endpoints;
    }

    public static IEndpointRouteBuilder MapTransportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/campus/v2/transport").RequireAuthorization().WithTags("Campus");

        group.MapGet("/routes", async (HttpContext context, ITransportService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetRoutesAsync(tenantId, ct));
        });

        group.MapPost("/routes/{routeId:guid}/optimize", async (Guid routeId, ITransportService service, CancellationToken ct) =>
            Results.Ok(await service.OptimizeRouteAsync(routeId, ct)));

        group.MapPost("/register", async (Guid studentId, Guid routeId, Guid stopId, ITransportService service, CancellationToken ct) =>
            Results.Ok(await service.RegisterStudentAsync(studentId, routeId, stopId, ct)));

        group.MapPost("/board", async (Guid studentId, Guid routeId, Guid stopId, string direction, ITransportService service, CancellationToken ct) =>
            Results.Ok(await service.LogBoardingAsync(studentId, routeId, stopId, direction, ct)));

        group.MapGet("/compliance-alerts", async (HttpContext context, ITransportService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetComplianceAlertsAsync(tenantId, ct));
        });

        group.MapGet("/analytics", async (HttpContext context, ITransportService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetAnalyticsAsync(tenantId, ct));
        });

        group.MapGet("/tracking/{vehicleId:guid}", async (Guid vehicleId, ITransportService service, CancellationToken ct) =>
            Results.Ok(await service.GetLiveVehicleLocationAsync(vehicleId, ct)));

        return endpoints;
    }
}
