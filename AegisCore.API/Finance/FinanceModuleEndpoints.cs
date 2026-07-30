using System.Security.Claims;
using AegisCore.Application.Finance;
using AegisCore.Infrastructure.Finance;

namespace AegisCore.API.Finance;

public static class FinanceModuleEndpoints
{
    public static IEndpointRouteBuilder MapFinanceModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/finance/v2").RequireAuthorization().WithTags("Finance");

        group.MapGet("/dashboard", async (HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
            Results.Ok(await finance.GetDashboardAsync(Tenant(context), ct)));

        group.MapGet("/reports/trial-balance", async (DateTime from, DateTime to, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(new { from, to, lines = await finance.GetTrialBalanceAsync(Tenant(context), from, to, ct) });
        });

        group.MapPost("/vouchers", async (HttpContext context, CreateVoucherCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var voucher = await finance.CreateVoucherAsync(Tenant(context), UserId(context), command, ct);
            return Results.Created($"/api/finance/v2/vouchers/{voucher.Id}", voucher);
        });

        group.MapPost("/vouchers/{id:guid}/submit", async (Guid id, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
            Results.Ok(await finance.SubmitVoucherAsync(Tenant(context), id, UserId(context), ct)));

        group.MapPost("/vouchers/{id:guid}/decision", async (Guid id, HttpContext context, VoucherDecisionCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.DecideVoucherAsync(Tenant(context), id, UserId(context), command, ct));
        });

        group.MapPost("/vouchers/{id:guid}/post", async (Guid id, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.PostVoucherAsync(Tenant(context), id, UserId(context), ct));
        });

        group.MapPost("/fee-demands", async (HttpContext context, CreateFeeDemandCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.CreateFeeDemandAsync(Tenant(context), command, ct));
        });

        group.MapPost("/payments", async (HttpContext context, RecordFinancePaymentCommand command, IFinanceModuleService finance, CancellationToken ct) =>
            Results.Ok(await finance.RecordPaymentAsync(Tenant(context), command, ct)));

        group.MapPut("/gateways/{slug}", (string _, HttpContext context) =>
            CanManageFinance(context)
                ? Results.StatusCode(StatusCodes.Status410Gone)
                : Results.Forbid());

        return endpoints;
    }

    private static Guid Tenant(HttpContext context)
        => Guid.TryParse(context.User.FindFirstValue("tenant_id") ?? context.User.FindFirstValue("tid"), out var tenant) && tenant != Guid.Empty
            ? tenant
            : throw new InvalidOperationException("Tenant claim is required.");

    private static Guid UserId(HttpContext context)
        => Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var user) && user != Guid.Empty
            ? user
            : throw new InvalidOperationException("User identity is required.");

    private static bool CanManageFinance(HttpContext context)
        => context.User.IsInRole("Finance") || context.User.IsInRole("CFO") || context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin");

}
