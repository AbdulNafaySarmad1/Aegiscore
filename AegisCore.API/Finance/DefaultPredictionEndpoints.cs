using AegisCore.Application.Finance;
using AegisCore.Infrastructure;

namespace AegisCore.API.Finance;

public static class DefaultPredictionEndpoints
{
    public static IEndpointRouteBuilder MapDefaultPredictionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/finance/v2/predictions").RequireAuthorization().WithTags("Finance");

        group.MapGet("/student/{studentId:guid}/{semesterId:guid}", async (Guid studentId, Guid semesterId, IDefaultPredictionService service, CancellationToken ct) =>
            Results.Ok(await service.AssessStudentRiskAsync(studentId, semesterId, ct)));

        group.MapPost("/assess-all/{semesterId:guid}", async (Guid semesterId, IDefaultPredictionService service, CancellationToken ct) =>
            Results.Ok(await service.AssessAllStudentsAsync(semesterId, ct)));

        group.MapPost("/intervention/{assessmentId:guid}", async (Guid assessmentId, IDefaultPredictionService service, CancellationToken ct) =>
            Results.Ok(await service.GenerateInterventionPlanAsync(assessmentId, ct)));

        group.MapGet("/forecast/{semesterId:guid}", async (Guid semesterId, IDefaultPredictionService service, CancellationToken ct) =>
            Results.Ok(await service.GenerateCashFlowForecastAsync(semesterId, ct)));

        group.MapGet("/alerts", async (HttpContext context, IDefaultPredictionService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetDefaultAlertQueueAsync(tenantId, ct));
        });

        group.MapGet("/outcomes/{semesterId:guid}", async (Guid semesterId, IDefaultPredictionService service, CancellationToken ct) =>
            Results.Ok(await service.GetInterventionOutcomesAsync(semesterId, ct)));

        return endpoints;
    }
}
