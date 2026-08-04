using AegisCore.Application.StudentLifecycle;

namespace AegisCore.API.Finance;

public static class StudentLifecycleModuleEndpoints
{
    public static IEndpointRouteBuilder MapVoiceAssistantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/student/v2/voice").RequireAuthorization().WithTags("StudentLifecycle");

        group.MapPost("/query", async (VoiceInputDto input, IVoiceAssistantService service, CancellationToken ct) =>
            Results.Ok(await service.ProcessVoiceQueryAsync(input, ct)));

        group.MapGet("/intents", async (HttpContext context, IVoiceAssistantService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetActiveIntentsAsync(tenantId, ct));
        });

        group.MapPost("/escalate/{sessionId:guid}", async (Guid sessionId, string reason, IVoiceAssistantService service, CancellationToken ct) =>
            Results.Ok(await service.EscalateToHumanAsync(sessionId, reason, ct)));

        return endpoints;
    }

    public static IEndpointRouteBuilder MapGamificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/student/v2/achievements").RequireAuthorization().WithTags("StudentLifecycle");

        group.MapGet("/detect", async (HttpContext context, IGamificationService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.DetectAchievementsAsync(tenantId, ct));
        });

        group.MapPost("/badges/{badgeId:guid}/award/{studentId:guid}", async (Guid badgeId, Guid studentId, IGamificationService service, CancellationToken ct) =>
            Results.Ok(await service.IssueBadgeAsync(badgeId, studentId, ct)));

        group.MapPost("/deans-list/{semesterId:guid}", async (Guid semesterId, IGamificationService service, CancellationToken ct) =>
            Results.Ok(await service.ProcessDeansListAsync(semesterId, ct)));

        group.MapGet("/leaderboard/{scope}", async (string scope, HttpContext context, IGamificationService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GenerateLeaderboardAsync(scope, tenantId, ct));
        });

        group.MapGet("/roadmap/{studentId:guid}", async (Guid studentId, IGamificationService service, CancellationToken ct) =>
            Results.Ok(await service.GetStudentRoadmapAsync(studentId, ct)));

        group.MapGet("/analytics", async (HttpContext context, IGamificationService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetAnalyticsAsync(tenantId, ct));
        });

        return endpoints;
    }

    public static IEndpointRouteBuilder MapMentalHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/student/v2/mental-health").RequireAuthorization().WithTags("StudentLifecycle");

        group.MapPost("/tickets", async (AnonymousTicketRequest request, IMentalHealthService service, CancellationToken ct) =>
            Results.Ok(await service.CreateTicketAsync(request, ct)));

        group.MapPost("/tickets/{ticketId:guid}/route", async (Guid ticketId, IMentalHealthService service, CancellationToken ct) =>
            Results.Ok(await service.RouteTicketToCounselorAsync(ticketId, ct)));

        group.MapGet("/counselor/{counselorId:guid}/queue", async (Guid counselorId, IMentalHealthService service, CancellationToken ct) =>
            Results.Ok(await service.GetCounselorQueueAsync(counselorId, ct)));

        group.MapPost("/tickets/{ticketId:guid}/close", async (Guid ticketId, string resolutionType, IMentalHealthService service, CancellationToken ct) =>
        {
            await service.CloseTicketAsync(ticketId, resolutionType, ct);
            return Results.Ok();
        });

        group.MapPost("/tickets/{ticketId:guid}/crisis-check", async (Guid ticketId, IMentalHealthService service, CancellationToken ct) =>
        {
            await service.CheckForCrisisFlagsAsync(ticketId, ct);
            return Results.Ok();
        });

        group.MapGet("/stats", async (HttpContext context, IMentalHealthService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetAggregateStatsAsync(tenantId, ct));
        });

        return endpoints;
    }

    public static IEndpointRouteBuilder MapForeignStudentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/student/v2/foreign").RequireAuthorization().WithTags("StudentLifecycle");

        group.MapGet("/expiry-alerts", async (HttpContext context, IForeignStudentService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetDocumentExpiryAlertsAsync(tenantId, ct));
        });

        group.MapGet("/mora/{semesterId:guid}", async (Guid semesterId, IForeignStudentService service, CancellationToken ct) =>
            Results.Ok(await service.GetMoraComplianceAsync(semesterId, ct)));

        group.MapPost("/enrollment-letter/{studentId:guid}", async (Guid studentId, string purpose, IForeignStudentService service, CancellationToken ct) =>
            Results.Ok(await service.GenerateEnrollmentLetterAsync(studentId, purpose, ct)));

        group.MapPost("/visa-renewal/{studentId:guid}", async (Guid studentId, IForeignStudentService service, CancellationToken ct) =>
            Results.Ok(await service.InitiateVisaRenewalAsync(studentId, ct)));

        group.MapGet("/compliance-breaches", async (HttpContext context, IForeignStudentService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetComplianceBreachesAsync(tenantId, ct));
        });

        group.MapGet("/dashboard", async (HttpContext context, IForeignStudentService service, CancellationToken ct) =>
        {
            var tenantId = context.Items["TenantId"] as Guid? ?? Guid.Empty;
            return Results.Ok(await service.GetIoDashboardAsync(tenantId, ct));
        });

        return endpoints;
    }
}
