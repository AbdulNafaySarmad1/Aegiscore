using System.Security.Claims;
using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.EntityFrameworkCore;

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

        group.MapGet("/security-posture", async (HttpContext context, AegisDbContext db, IPqcSignatureService pqc, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var seal = await db.FinanceAuditSeals.AsNoTracking()
                .OrderByDescending(x => x.SealedAt).FirstOrDefaultAsync(ct);
            var chainIntegrity = seal is null ? "amber" : VerifySeal(Tenant(context), seal, pqc) ? "green" : "red";
            return Results.Ok(new
            {
                chainIntegrity,
                lastSealTimestamp = seal?.SealedAt,
                merkleRoot = seal is null ? null : Convert.ToHexString(seal.MerkleRoot).ToLowerInvariant(),
                signatureAlgorithm = seal?.Algorithm,
                keyId = seal?.KeyId,
                eventCount = seal?.EventCount ?? 0
            });
        });

        group.MapGet("/vouchers/{id:guid}/verification", async (Guid id, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            var voucher = await db.Set<FinanceVoucher>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.TenantId == Tenant(context) && x.Id == id, ct);
            if (voucher is null) return Results.NotFound();
            return Results.Ok(new
            {
                voucher.Id,
                voucher.VoucherNumber,
                voucher.Status,
                voucher.PqcAlgorithm,
                voucher.PqcKeyId,
                Signature = voucher.PqcSignature is null ? null : Convert.ToBase64String(voucher.PqcSignature),
                PublicKey = voucher.PqcPublicKey is null ? null : Convert.ToBase64String(voucher.PqcPublicKey)
            });
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

    private static bool VerifySeal(Guid tenantId, FinanceAuditSeal seal, IPqcSignatureService pqc)
    {
        var payload = System.Text.Encoding.UTF8.GetBytes($"{tenantId:D}|{seal.PeriodStart:O}|{seal.PeriodEnd:O}|{seal.EventCount}|{Convert.ToHexString(seal.MerkleRoot)}");
        return pqc.Verify(seal.Algorithm, payload, seal.Signature, seal.PublicKey);
    }

}
