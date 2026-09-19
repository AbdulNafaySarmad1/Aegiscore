using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using AegisCore.Domain;
using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Finance.Gateways;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using QRCoder;

namespace AegisCore.API.Finance;

public static partial class FinanceModuleEndpoints
{
    public static IEndpointRouteBuilder MapFinanceModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/finance/v2").RequireAuthorization().WithTags("Finance");

        group.MapGet("/dashboard", async (HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
            Results.Ok(await finance.GetDashboardAsync(Tenant(context), ct)));

        group.MapGet("/dashboard/executive", async (HttpContext context, AegisDbContext db, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanViewExecutiveFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var now = DateTime.UtcNow;
            var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);
            var ledger = await (
                from line in db.JournalEntryLines.AsNoTracking()
                join entry in db.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
                join account in db.ChartOfAccounts.AsNoTracking() on line.ChartOfAccountId equals account.Id
                where line.TenantId == tenantId && entry.TenantId == tenantId && account.TenantId == tenantId
                      && entry.IsPosted && !entry.IsSandbox && entry.EntryDate >= periodStart
                select new { entry.EntryDate, account.AccountType, account.AccountName, account.CashFlowCategory, line.CostCenterId, line.Debit, line.Credit }
            ).ToListAsync(ct);
            var trend = Enumerable.Range(0, 12).Select(offset =>
            {
                var month = periodStart.AddMonths(offset);
                var rows = ledger.Where(x => x.EntryDate.Year == month.Year && x.EntryDate.Month == month.Month).ToArray();
                return new
                {
                    month = month.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
                    revenue = rows.Where(x => x.AccountType == AccountType.Revenue).Sum(x => x.Credit - x.Debit),
                    expenses = rows.Where(x => x.AccountType == AccountType.Expense).Sum(x => x.Debit - x.Credit),
                    postings = rows.Length
                };
            }).ToArray();
            var demands = await db.FeeDemands.AsNoTracking().Where(x => x.TenantId == tenantId)
                .Select(x => new { x.Status, x.TotalAmount, x.PaidAmount, x.OutstandingAmount, x.DueDate }).ToListAsync(ct);
            var expenseCategories = ledger.Where(x => x.AccountType == AccountType.Expense)
                .GroupBy(x => x.AccountName).Select(x => new { category = x.Key, amount = x.Sum(y => y.Debit - y.Credit) })
                .OrderByDescending(x => x.amount).Take(10).ToArray();
            var costCenters = await db.CostCenters.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.Name).ToListAsync(ct);
            var budgetBurn = costCenters.Select(costCenter =>
            {
                var spent = ledger.Where(x => x.CostCenterId == costCenter.Id && x.AccountType == AccountType.Expense).Sum(x => x.Debit - x.Credit);
                return new { costCenterId = costCenter.Id, name = costCenter.Name, allocated = costCenter.BudgetAllocation, spent, consumedPercent = costCenter.BudgetAllocation <= 0m ? 0m : Math.Round(spent / costCenter.BudgetAllocation * 100m, 2) };
            }).ToArray();
            var currencyExposure = await db.FinanceForeignCurrencyTransactions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && !x.IsSettled)
                .GroupBy(x => x.ForeignCurrency)
                .Select(x => new { currency = x.Key, foreignAmount = x.Sum(y => y.ForeignAmount), functionalAmount = x.Sum(y => y.FunctionalAmount) })
                .OrderByDescending(x => x.functionalAmount).ToListAsync(ct);
            var cashFlow = new[] { "OPERATING", "INVESTING", "FINANCING" }.Select(category => new
            {
                category,
                amount = ledger.Where(x => x.CashFlowCategory == category).Sum(x => x.Credit - x.Debit)
            }).ToArray();
            var unclassifiedCashFlow = ledger.Where(x => x.CashFlowCategory == null && x.AccountType != AccountType.Asset).Sum(x => x.Credit - x.Debit);
            var cash = await finance.GetCashPositionAsync(tenantId, now, ct);
            return Results.Ok(new
            {
                kpis = new
                {
                    revenue = ledger.Where(x => x.AccountType == AccountType.Revenue).Sum(x => x.Credit - x.Debit),
                    expenses = ledger.Where(x => x.AccountType == AccountType.Expense).Sum(x => x.Debit - x.Credit),
                    cashPosition = cash.TotalBalance
                },
                trend,
                feeFunnel = new
                {
                    demanded = demands.Sum(x => x.TotalAmount),
                    collected = demands.Sum(x => x.PaidAmount),
                    overdue = demands.Where(x => x.DueDate < now && x.OutstandingAmount > 0m).Sum(x => x.OutstandingAmount),
                    open = demands.Count(x => x.OutstandingAmount > 0m),
                    paid = demands.Count(x => x.Status == "PAID")
                },
                expenseCategories,
                budgetBurn,
                currencyExposure,
                cashFlow,
                unclassifiedCashFlow
            });
        });

        group.MapGet("/dashboard/operations", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var vouchers = await db.FinanceVouchers.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.VoucherNumber, x.VoucherType, x.Status, x.Currency, x.TotalAmount, x.Description, x.CreatedAt })
                .Take(500).ToListAsync(ct);
            var collectionsFrom = DateTime.UtcNow.Date.AddDays(-30);
            var collections = await db.FinancePayments.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Status == "SETTLED" && x.SettledAt >= collectionsFrom)
                .GroupBy(x => x.SettledAt!.Value.Date)
                .Select(x => new { date = x.Key, amount = x.Sum(y => y.Amount), count = x.Count() }).OrderBy(x => x.date).ToListAsync(ct);
            var liveFeed = await db.PaymentGatewayEvents.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.CreatedAt).Take(50)
                .Select(x => new { x.Id, x.EventType, x.GatewaySlug, x.ReferenceId, x.Amount, x.Currency, x.Status, x.IsSandbox, x.CreatedAt }).ToListAsync(ct);
            return Results.Ok(new
            {
                pipeline = vouchers.GroupBy(x => x.Status).Select(x => new { status = x.Key, count = x.Count(), total = x.Sum(y => y.TotalAmount), vouchers = x.Take(50) }),
                approvals = vouchers.Where(x => x.Status == FinanceStates.PendingApproval).Take(100),
                collections,
                liveFeed
            });
        });

        group.MapGet("/dashboard/department/{departmentId:guid}", async (Guid departmentId, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            var tenantId = Tenant(context);
            if (!CanManageFinance(context) && DepartmentId(context) != departmentId) return Results.Forbid();
            var department = await db.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == departmentId, ct);
            if (department is null) return Results.NotFound();
            var costCenters = await db.CostCenters.AsNoTracking().Where(x => x.TenantId == tenantId && x.DepartmentId == departmentId).ToListAsync(ct);
            var costCenterIds = costCenters.Select(x => x.Id).ToArray();
            var postedSpend = await (
                from line in db.JournalEntryLines.AsNoTracking()
                join entry in db.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
                join account in db.ChartOfAccounts.AsNoTracking() on line.ChartOfAccountId equals account.Id
                where line.TenantId == tenantId && entry.TenantId == tenantId && account.TenantId == tenantId
                      && entry.IsPosted && !entry.IsSandbox && line.CostCenterId != null && costCenterIds.Contains(line.CostCenterId.Value)
                      && account.AccountType == AccountType.Expense
                select new { account.AccountName, line.Debit, line.Credit }
            ).ToListAsync(ct);
            var committed = await db.FinanceVoucherLines.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.CostCenterId != null && costCenterIds.Contains(x.CostCenterId.Value)
                    && (x.Voucher.Status == FinanceStates.PendingApproval || x.Voucher.Status == FinanceStates.Approved))
                .SumAsync(x => x.Debit, ct);
            var allocated = costCenters.Sum(x => x.BudgetAllocation);
            var spent = postedSpend.Sum(x => x.Debit - x.Credit);
            var pettyCash = await db.FinancePettyCashFunds.AsNoTracking().Where(x => x.TenantId == tenantId && x.DepartmentId == departmentId && x.IsActive)
                .Select(x => new { x.Currency, x.FloatAmount, x.CurrentBalance }).FirstOrDefaultAsync(ct);
            var approvals = await db.FinanceVouchers.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Status == FinanceStates.PendingApproval
                    && x.Lines.Any(line => line.CostCenterId != null && costCenterIds.Contains(line.CostCenterId.Value)))
                .OrderBy(x => x.CreatedAt).Take(50)
                .Select(x => new { x.Id, x.VoucherNumber, x.VoucherType, x.Currency, x.TotalAmount, x.Description, x.CreatedAt }).ToListAsync(ct);
            var categories = postedSpend.GroupBy(x => x.AccountName)
                .Select(x => new { category = x.Key, amount = x.Sum(y => y.Debit - y.Credit) }).OrderByDescending(x => x.amount).ToArray();
            return Results.Ok(new
            {
                department = new { department.Id, department.Name, department.Code },
                budget = new { allocated, spent, committed, available = allocated - spent - committed },
                pettyCash,
                approvals,
                categories
            });
        });

        group.MapGet("/student-portal/{studentId:guid}", async (Guid studentId, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            var tenantId = Tenant(context);
            if (!CanManageFinance(context) && StudentId(context) != studentId) return Results.Forbid();
            var exists = await db.StudentProfiles.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.StudentId == studentId, ct);
            if (!exists) return Results.NotFound();
            var demands = await db.FeeDemands.AsNoTracking().Where(x => x.TenantId == tenantId && x.StudentId == studentId)
                .OrderByDescending(x => x.IssuedAt).Select(x => new { x.Id, x.DemandNumber, x.Currency, x.TotalAmount, x.PaidAmount, x.OutstandingAmount, x.Status, x.IssuedAt, x.DueDate }).ToListAsync(ct);
            var demandIds = demands.Select(x => x.Id).ToArray();
            var installments = await db.FeeInstallments.AsNoTracking().Where(x => x.TenantId == tenantId && demandIds.Contains(x.FeeDemandId))
                .OrderBy(x => x.DueDate).Select(x => new { x.Id, x.FeeDemandId, x.Sequence, x.Amount, x.PaidAmount, x.PenaltyAmount, x.DueDate, x.Status }).ToListAsync(ct);
            var payments = await db.FinancePayments.AsNoTracking().Where(x => x.TenantId == tenantId && demandIds.Contains(x.FeeDemandId))
                .OrderByDescending(x => x.CreatedAt).Select(x => new { x.Id, x.FeeDemandId, x.Amount, x.Method, x.GatewaySlug, x.ExternalReference, x.Status, x.CreatedAt, x.SettledAt }).ToListAsync(ct);
            var awards = await db.FinanceScholarshipAwards.AsNoTracking().Where(x => x.TenantId == tenantId && x.StudentId == studentId)
                .OrderByDescending(x => x.AwardedAt).Select(x => new { x.Id, x.ScholarshipType, x.CoverageType, x.Amount, x.DisbursementMethod, x.Status, x.AwardedAt, x.RenewalReviewAt }).ToListAsync(ct);
            return Results.Ok(new { studentId, demands, installments, payments, awards });
        });

        group.MapGet("/configuration", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var configuration = await db.TenantFinanceConfigurations.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == Tenant(context), ct);
            return configuration is null ? Results.NotFound() : Results.Ok(configuration);
        });

        group.MapGet("/accounting/context", async (HttpContext context, IAccountingStandardsEngine accounting, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await accounting.GetContextAsync(Tenant(context), ct));
        });

        group.MapPost("/accounting/reporting-entities", async (HttpContext context, CreateReportingEntityCommand command, IAccountingStandardsEngine accounting, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var entity = await accounting.CreateReportingEntityAsync(Tenant(context), command, ct);
            return Results.Created($"/api/finance/v2/accounting/reporting-entities/{entity.Id:D}", entity);
        });

        group.MapPost("/accounting/restatement-rules", async (HttpContext context, CreateRestatementRuleCommand command, IAccountingStandardsEngine accounting, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var rule = await accounting.CreateRestatementRuleAsync(Tenant(context), command, ct);
            return Results.Created($"/api/finance/v2/accounting/restatement-rules/{rule.Id:D}", rule);
        });

        group.MapPost("/accounting/restatements/run", async (HttpContext context, RunRestatementCommand command, IAccountingStandardsEngine accounting, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await accounting.RunRestatementAsync(Tenant(context), UserId(context), command, ct));
        });

        group.MapPost("/accounting/standard/switch", async (HttpContext context, SwitchAccountingStandardCommand command, IAccountingStandardsEngine accounting, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await accounting.SwitchStandardAsync(Tenant(context), UserId(context), command, ct));
        });

        group.MapPost("/accounting/consolidation-groups", async (HttpContext context, CreateConsolidationGroupCommand command, IAccountingStandardsEngine accounting, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var consolidation = await accounting.CreateConsolidationGroupAsync(Tenant(context), command, ct);
            return Results.Created($"/api/finance/v2/accounting/consolidation-groups/{consolidation.Id:D}", consolidation);
        });

        group.MapPost("/accounting/elimination-rules", async (HttpContext context, CreateEliminationRuleCommand command, IAccountingStandardsEngine accounting, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var rule = await accounting.CreateEliminationRuleAsync(Tenant(context), command, ct);
            return Results.Created($"/api/finance/v2/accounting/elimination-rules/{rule.Id:D}", rule);
        });

        group.MapPost("/accounting/consolidations/run", async (HttpContext context, RunConsolidationCommand command, IAccountingStandardsEngine accounting, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await accounting.RunConsolidationAsync(Tenant(context), UserId(context), command, ct));
        });

        group.MapGet("/reports/trial-balance", async (DateTime from, DateTime to, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(new { from, to, lines = await finance.GetTrialBalanceAsync(Tenant(context), from, to, ct) });
        });

        group.MapGet("/reports/cash-position", async (DateTime? asOf, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.GetCashPositionAsync(Tenant(context), asOf ?? DateTime.UtcNow, ct));
        });

        group.MapGet("/reports/statement-of-financial-position", async (DateTime? asOf, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.GetFinancialPositionAsync(Tenant(context), asOf ?? DateTime.UtcNow, ct));
        });

        group.MapGet("/reports/profit-loss", async (DateTime from, DateTime to, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.GetProfitLossAsync(Tenant(context), from, to, ct));
        });

        group.MapGet("/reports/receivables-aging", async (DateTime? asOf, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var report = await finance.GetReceivablesAgingAsync(Tenant(context), asOf ?? DateTime.UtcNow, ct);
            return Results.Ok(report);
        });

        group.MapPost("/reports/{reportType}/signed-json", async (string reportType, DateTime? from, DateTime? to, DateTime? asOf, HttpContext context, IFinanceModuleService finance, AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var normalized = reportType.Trim().ToLowerInvariant();
            if (normalized is not ("trial-balance" or "cash-position" or "statement-of-financial-position" or "profit-loss" or "receivables-aging"))
                return Results.UnprocessableEntity(new { success = false, error = "Unsupported finance report type." });
            object report = normalized switch
            {
                "trial-balance" => new { from = from ?? DateTime.UtcNow.Date, to = to ?? DateTime.UtcNow, lines = await finance.GetTrialBalanceAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct) },
                "cash-position" => await finance.GetCashPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                "statement-of-financial-position" => await finance.GetFinancialPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                "profit-loss" => await finance.GetProfitLossAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct),
                "receivables-aging" => await finance.GetReceivablesAgingAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                _ => throw new InvalidOperationException("Report type validation failed.")
            };
            var payload = JsonSerializer.SerializeToUtf8Bytes(report, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = false });
            var artifactId = Guid.NewGuid();
            var signature = await pqc.SignAsync(tenantId, "finance-report", payload, archival: false, ct);
            var storageKey = $"finance-reports/{tenantId:D}/{DateTime.UtcNow:yyyy/MM}/{artifactId:N}.json";
            await storage.UploadAsync(storageKey, new MemoryStream(payload), "application/json", ct);
            var artifact = new FinanceReportArtifact
            {
                Id = artifactId, TenantId = tenantId, ReportType = normalized, Format = "JSON",
                PeriodLabel = $"{from:O}|{to:O}|{asOf:O}", StorageKey = storageKey,
                ContentHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(), PqcAlgorithm = signature.Algorithm,
                PqcKeyId = signature.KeyId, PqcSignature = signature.Signature, PqcPublicKey = signature.PublicKey,
                GeneratedByUserId = UserId(context)
            };
            db.FinanceReportArtifacts.Add(artifact);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { artifact.Id, artifact.ReportType, artifact.Format, artifact.StorageKey, artifact.ContentHash, artifact.PqcAlgorithm, artifact.PqcKeyId, signature = Convert.ToBase64String(artifact.PqcSignature), publicKey = Convert.ToBase64String(artifact.PqcPublicKey), verification = $"/api/finance/v2/reports/signed/{artifact.Id:D}/verify" });
        });

        group.MapGet("/reports/signed/{id:guid}/verify", async (Guid id, HttpContext context, AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, CancellationToken ct) =>
        {
            var artifact = await db.FinanceReportArtifacts.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == Tenant(context) && x.Id == id, ct);
            if (artifact is null) return Results.NotFound();
            var content = await storage.DownloadAsync(artifact.StorageKey, ct);
            if (content is null) return Results.Ok(new { verified = false, reason = "Artifact is not available in storage." });
            await using (content) using (var buffer = new MemoryStream())
            {
                await content.CopyToAsync(buffer, ct);
                var bytes = buffer.ToArray();
                var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                var verified = hash == artifact.ContentHash && pqc.Verify(artifact.PqcAlgorithm, bytes, artifact.PqcSignature, artifact.PqcPublicKey);
                return Results.Ok(new { verified, artifact.Id, artifact.ContentHash, calculatedHash = hash, artifact.PqcAlgorithm, artifact.PqcKeyId });
            }
        });

        group.MapPost("/reports/{reportType}/signed-csv", async (string reportType, DateTime? from, DateTime? to, DateTime? asOf, HttpContext context, IFinanceModuleService finance, AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var normalized = reportType.Trim().ToLowerInvariant();
            if (normalized is not ("trial-balance" or "cash-position" or "statement-of-financial-position" or "profit-loss" or "receivables-aging"))
                return Results.UnprocessableEntity(new { success = false, error = "Unsupported finance report type." });
            object report = normalized switch
            {
                "trial-balance" => new { from = from ?? DateTime.UtcNow.Date, to = to ?? DateTime.UtcNow, lines = await finance.GetTrialBalanceAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct) },
                "cash-position" => await finance.GetCashPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                "statement-of-financial-position" => await finance.GetFinancialPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                "profit-loss" => await finance.GetProfitLossAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct),
                "receivables-aging" => await finance.GetReceivablesAgingAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                _ => throw new InvalidOperationException("Report type validation failed.")
            };
            var payload = ReportCsv(report);
            var artifactId = Guid.NewGuid();
            var signature = await pqc.SignAsync(tenantId, "finance-report", payload, archival: false, ct);
            var storageKey = $"finance-reports/{tenantId:D}/{DateTime.UtcNow:yyyy/MM}/{artifactId:N}.csv";
            await storage.UploadAsync(storageKey, new MemoryStream(payload), "text/csv; charset=utf-8", ct);
            var artifact = new FinanceReportArtifact
            {
                Id = artifactId, TenantId = tenantId, ReportType = normalized, Format = "CSV", PeriodLabel = $"{from:O}|{to:O}|{asOf:O}", StorageKey = storageKey,
                ContentHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(), PqcAlgorithm = signature.Algorithm, PqcKeyId = signature.KeyId, PqcSignature = signature.Signature, PqcPublicKey = signature.PublicKey, GeneratedByUserId = UserId(context)
            };
            db.FinanceReportArtifacts.Add(artifact);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { artifact.Id, artifact.ReportType, artifact.Format, artifact.StorageKey, artifact.ContentHash, artifact.PqcAlgorithm, artifact.PqcKeyId, signature = Convert.ToBase64String(artifact.PqcSignature), publicKey = Convert.ToBase64String(artifact.PqcPublicKey), verification = $"/api/finance/v2/reports/signed/{artifact.Id:D}/verify" });
        });

        group.MapPost("/reports/{reportType}/signed-xlsx", async (string reportType, DateTime? from, DateTime? to, DateTime? asOf, HttpContext context, IFinanceModuleService finance, AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var normalized = reportType.Trim().ToLowerInvariant();
            if (normalized is not ("trial-balance" or "cash-position" or "statement-of-financial-position" or "profit-loss" or "receivables-aging"))
                return Results.UnprocessableEntity(new { success = false, error = "Unsupported finance report type." });
            object report = normalized switch
            {
                "trial-balance" => new { from = from ?? DateTime.UtcNow.Date, to = to ?? DateTime.UtcNow, lines = await finance.GetTrialBalanceAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct) },
                "cash-position" => await finance.GetCashPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                "statement-of-financial-position" => await finance.GetFinancialPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                "profit-loss" => await finance.GetProfitLossAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct),
                "receivables-aging" => await finance.GetReceivablesAgingAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                _ => throw new InvalidOperationException("Report type validation failed.")
            };
            var payload = ReportXlsx(report);
            var artifactId = Guid.NewGuid();
            var signature = await pqc.SignAsync(tenantId, "finance-report", payload, archival: false, ct);
            var storageKey = $"finance-reports/{tenantId:D}/{DateTime.UtcNow:yyyy/MM}/{artifactId:N}.xlsx";
            await storage.UploadAsync(storageKey, new MemoryStream(payload), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ct);
            var artifact = new FinanceReportArtifact
            {
                Id = artifactId, TenantId = tenantId, ReportType = normalized, Format = "XLSX", PeriodLabel = $"{from:O}|{to:O}|{asOf:O}", StorageKey = storageKey,
                ContentHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(), PqcAlgorithm = signature.Algorithm, PqcKeyId = signature.KeyId, PqcSignature = signature.Signature, PqcPublicKey = signature.PublicKey, GeneratedByUserId = UserId(context)
            };
            db.FinanceReportArtifacts.Add(artifact);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { artifact.Id, artifact.ReportType, artifact.Format, artifact.StorageKey, artifact.ContentHash, artifact.PqcAlgorithm, artifact.PqcKeyId, signature = Convert.ToBase64String(artifact.PqcSignature), publicKey = Convert.ToBase64String(artifact.PqcPublicKey), verification = $"/api/finance/v2/reports/signed/{artifact.Id:D}/verify" });
        });

        group.MapPost("/reports/{reportType}/signed-pdf", async (string reportType, DateTime? from, DateTime? to, DateTime? asOf, HttpContext context, IFinanceModuleService finance, AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var normalized = reportType.Trim().ToLowerInvariant();
            if (normalized is not ("trial-balance" or "cash-position" or "statement-of-financial-position" or "profit-loss" or "receivables-aging"))
                return Results.UnprocessableEntity(new { success = false, error = "Unsupported finance report type." });
            object report = normalized switch
            {
                "trial-balance" => new { from = from ?? DateTime.UtcNow.Date, to = to ?? DateTime.UtcNow, lines = await finance.GetTrialBalanceAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct) },
                "cash-position" => await finance.GetCashPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                "statement-of-financial-position" => await finance.GetFinancialPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                "profit-loss" => await finance.GetProfitLossAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct),
                "receivables-aging" => await finance.GetReceivablesAgingAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
                _ => throw new InvalidOperationException("Report type validation failed.")
            };
            var artifactId = Guid.NewGuid();
            var verificationUrl = $"/api/finance/v2/reports/signed/{artifactId:D}/verify";
            var payload = ReportPdf(report, verificationUrl);
            var signature = await pqc.SignAsync(tenantId, "finance-report", payload, archival: false, ct);
            var storageKey = $"finance-reports/{tenantId:D}/{DateTime.UtcNow:yyyy/MM}/{artifactId:N}.pdf";
            await storage.UploadAsync(storageKey, new MemoryStream(payload), "application/pdf", ct);
            var artifact = new FinanceReportArtifact
            {
                Id = artifactId, TenantId = tenantId, ReportType = normalized, Format = "PDF", PeriodLabel = $"{from:O}|{to:O}|{asOf:O}", StorageKey = storageKey,
                ContentHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(), PqcAlgorithm = signature.Algorithm, PqcKeyId = signature.KeyId, PqcSignature = signature.Signature, PqcPublicKey = signature.PublicKey, GeneratedByUserId = UserId(context)
            };
            db.FinanceReportArtifacts.Add(artifact);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { artifact.Id, artifact.ReportType, artifact.Format, artifact.StorageKey, artifact.ContentHash, artifact.PqcAlgorithm, artifact.PqcKeyId, signature = Convert.ToBase64String(artifact.PqcSignature), publicKey = Convert.ToBase64String(artifact.PqcPublicKey), verification = verificationUrl });
        });

        group.MapPost("/reports/{reportType}/signed-xbrl", async (string reportType, DateTime? from, DateTime? to, DateTime? asOf, HttpContext context, IFinanceModuleService finance, AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, CancellationToken ct) =>
        {
            return await CreateSignedXmlReport(reportType, "XBRL", ".xbrl", "application/xml", false, from, to, asOf, context, finance, db, pqc, storage, ct);
        });

        group.MapPost("/reports/{reportType}/signed-ixbrl", async (string reportType, DateTime? from, DateTime? to, DateTime? asOf, HttpContext context, IFinanceModuleService finance, AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, CancellationToken ct) =>
        {
            return await CreateSignedXmlReport(reportType, "IXBRL", ".html", "text/html; charset=utf-8", true, from, to, asOf, context, finance, db, pqc, storage, ct);
        });
        group.MapGet("/regulatory/schemas", async (HttpContext context, FinanceRegulatoryReportService regulatory, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await regulatory.ListAsync(Tenant(context), ct));
        });
        group.MapPost("/regulatory/schemas", async (HttpContext context, ConfigureRegulatorySchemaCommand command, FinanceRegulatoryReportService regulatory, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await regulatory.ConfigureAsync(Tenant(context), command, ct));
        });
        group.MapPost("/regulatory/{code}/generate", async (string code, HttpContext context, GenerateRegulatorySubmissionCommand command, FinanceRegulatoryReportService regulatory, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var submission = await regulatory.GenerateAsync(Tenant(context), UserId(context), code, command, ct);
            return Results.Ok(new { submission.Id, submission.RegulatorySchemaId, submission.PeriodLabel, submission.StorageKey, submission.ContentHash, submission.PqcAlgorithm, submission.PqcKeyId, signature = Convert.ToBase64String(submission.PqcSignature), publicKey = Convert.ToBase64String(submission.PqcPublicKey), verification = $"/api/finance/v2/regulatory/submissions/{submission.Id:D}/verify" });
        });
        group.MapGet("/regulatory/submissions/{id:guid}/verify", async (Guid id, HttpContext context, FinanceRegulatoryReportService regulatory, CancellationToken ct) =>
            Results.Ok(new { verified = await regulatory.VerifyAsync(Tenant(context), id, ct) }));

        group.MapGet("/report-profiles", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await db.FinanceReportProfiles.AsNoTracking().Where(x => x.TenantId == Tenant(context)).OrderBy(x => x.Name).ToListAsync(ct));
        });

        group.MapPost("/report-profiles", async (HttpContext context, CreateFinanceReportProfileCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var standard = command.TargetStandard.Trim().ToUpperInvariant().Replace(' ', '_');
            var fundFilter = command.FundFilter.Trim().ToUpperInvariant();
            var period = command.PeriodType.Trim().ToUpperInvariant();
            var currency = command.ReportCurrency.Trim().ToUpperInvariant();
            var rateMethod = command.RateMethod.Trim().ToUpperInvariant();
            var schedule = command.Schedule.Trim().ToUpperInvariant();
            var delivery = command.Delivery.Trim().ToUpperInvariant();
            var format = command.Format.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Trim().Length > 120 || standard is not ("IFRS" or "US_GAAP" or "FRS_102" or "IPSAS" or "AAOIFI") || fundFilter is not ("RESTRICTED" or "UNRESTRICTED" or "ALL") || period is not ("FISCAL_YEAR" or "CALENDAR_YEAR" or "GRANT_PERIOD" or "CUSTOM") || !ValidCurrency(currency) || rateMethod is not ("CLOSING" or "AVERAGE" or "TRANSACTION") || schedule is not ("MONTHLY" or "QUARTERLY" or "ANNUALLY" or "ON_DEMAND") || delivery is not ("EMAIL" or "IN_APP" or "SFTP") || format is not ("JSON" or "CSV" or "XLSX" or "PDF" or "XBRL" or "IXBRL") || command.ComparativeYears is < 1 or > 5 || (standard == "IPSAS" && !command.BudgetVsActual))
                return Results.UnprocessableEntity(new { success = false, error = "Report profile fields are invalid." });
            var profile = new FinanceReportProfile
            {
                Id = Guid.NewGuid(), TenantId = Tenant(context), Name = command.Name.Trim(), TargetStandard = standard, FunderName = command.FunderName?.Trim(), FundFilter = fundFilter,
                PeriodType = period, ReportCurrency = currency, RateMethod = rateMethod, Schedule = schedule, Delivery = delivery, Format = format, ComparativeYears = command.ComparativeYears,
                BudgetVsActual = command.BudgetVsActual, RestrictedFundUtilisation = command.RestrictedFundUtilisation, GrantDrawdown = command.GrantDrawdown, PqcEnabled = command.PqcEnabled, CreatedByUserId = UserId(context)
            };
            db.FinanceReportProfiles.Add(profile);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/report-profiles/{profile.Id}", profile);
        });

        group.MapPost("/report-profiles/{id:guid}/run", async (Guid id, string reportType, DateTime? from, DateTime? to, DateTime? asOf, HttpContext context, IFinanceModuleService finance, AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var profile = await db.FinanceReportProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.IsActive, ct);
            if (profile is null) return Results.NotFound();
            var normalized = reportType.Trim().ToLowerInvariant();
            if (normalized is not ("trial-balance" or "cash-position" or "statement-of-financial-position" or "profit-loss" or "receivables-aging"))
                return Results.UnprocessableEntity(new { success = false, error = "Unsupported finance report type." });
            var report = await BuildReportAsync(normalized, tenantId, from, to, asOf, finance, ct);
            var artifactId = Guid.NewGuid();
            var verificationUrl = $"/api/finance/v2/reports/signed/{artifactId:D}/verify";
            var (payload, extension, contentType) = profile.Format switch
            {
                "JSON" => (JsonSerializer.SerializeToUtf8Bytes(report, new JsonSerializerOptions(JsonSerializerDefaults.Web)), ".json", "application/json"),
                "CSV" => (ReportCsv(report), ".csv", "text/csv; charset=utf-8"),
                "XLSX" => (ReportXlsx(report), ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
                "PDF" => (ReportPdf(report, verificationUrl), ".pdf", "application/pdf"),
                "XBRL" => (ReportXbrl(report, verificationUrl, false), ".xbrl", "application/xml"),
                "IXBRL" => (ReportXbrl(report, verificationUrl, true), ".html", "text/html; charset=utf-8"),
                _ => throw new InvalidOperationException("Report profile format is invalid.")
            };
            var signature = profile.PqcEnabled
                ? await pqc.SignAsync(tenantId, "finance-report", payload, archival: false, ct)
                : new PqcSignatureResult("UNSIGNED", "", [], []);
            var storageKey = $"finance-reports/{tenantId:D}/{DateTime.UtcNow:yyyy/MM}/{artifactId:N}{extension}";
            await storage.UploadAsync(storageKey, new MemoryStream(payload), contentType, ct);
            var artifact = new FinanceReportArtifact
            {
                Id = artifactId, TenantId = tenantId, ReportType = normalized, Format = profile.Format, PeriodLabel = $"{from:O}|{to:O}|{asOf:O}", StorageKey = storageKey,
                ContentHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(), PqcAlgorithm = signature.Algorithm, PqcKeyId = signature.KeyId, PqcSignature = signature.Signature, PqcPublicKey = signature.PublicKey, GeneratedByUserId = UserId(context)
            };
            db.FinanceReportArtifacts.Add(artifact);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { profileId = profile.Id, profile.Name, artifactId = artifact.Id, artifact.ReportType, artifact.Format, artifact.StorageKey, artifact.ContentHash, artifact.PqcAlgorithm, artifact.PqcKeyId, delivery = profile.Delivery, deliveryStatus = "GENERATED", signature = Convert.ToBase64String(artifact.PqcSignature), publicKey = Convert.ToBase64String(artifact.PqcPublicKey), verification = verificationUrl });
        });

        group.MapPost("/report-profiles/{id:guid}/deactivate", async (Guid id, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var profile = await db.FinanceReportProfiles.SingleOrDefaultAsync(x => x.TenantId == Tenant(context) && x.Id == id, ct);
            if (profile is null) return Results.NotFound();
            profile.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { profile.Id, profile.IsActive });
        });

        group.MapGet("/security-posture", async (HttpContext context, AegisDbContext db, IPqcSignatureService pqc, FinanceLedgerIntegrityService ledger, IConfiguration configuration, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var seal = await db.FinanceAuditSeals.AsNoTracking()
                .OrderByDescending(x => x.SealedAt).FirstOrDefaultAsync(ct);
            var keys = await db.FinancePqcKeys.AsNoTracking().Where(x => x.TenantId == Tenant(context))
                .OrderByDescending(x => x.CreatedAt).Select(x => new { x.KeyId, x.Purpose, x.Algorithm, x.IsActive, x.CreatedAt, x.ExpiresAt, x.LastUsedAt }).ToListAsync(ct);
            var ledgerResult = await ledger.VerifyDetailedAsync(Tenant(context), ct);
            var ledgerVerified = ledgerResult.IsValid;
            var now = DateTime.UtcNow;
            var deadKeyDays = configuration.GetValue("Pqc:DeadKeyDays", 30);
            var chainIntegrity = !ledgerVerified ? "red" : seal is null ? "amber" : VerifySeal(Tenant(context), seal, pqc) ? "green" : "red";
            var unsignedLedgerEntries = await db.JournalEntries.AsNoTracking().CountAsync(x => x.IsPosted && x.EntryHash != null && !db.FinanceLedgerSignatures.Any(s => s.JournalEntryId == x.Id), ct);
            return Results.Ok(new
            {
                chainIntegrity,
                lastSealTimestamp = seal?.SealedAt,
                merkleRoot = seal is null ? null : Convert.ToHexString(seal.MerkleRoot).ToLowerInvariant(),
                signatureAlgorithm = seal?.Algorithm,
                keyId = seal?.KeyId,
                eventCount = seal?.EventCount ?? 0,
                ledgerVerified,
                unsignedLedgerEntries,
                failedVerificationAlerts = ledgerResult.Failures,
                keys = keys.Select(x => new { x.KeyId, x.Purpose, x.Algorithm, x.IsActive, x.CreatedAt, x.ExpiresAt, x.LastUsedAt, daysToExpiry = Math.Max(0, (x.ExpiresAt - now).Days) }),
                deadKeys = keys.Where(x => x.IsActive && PqcKeyHealth.IsDead(x.CreatedAt, x.LastUsedAt, now, deadKeyDays)).Select(x => x.KeyId)
            });
        });

        group.MapGet("/security/ledger-archive", async (DateTime? from, DateTime? to, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var start = from ?? DateTime.UnixEpoch;
            var end = to ?? DateTime.UtcNow;
            if (start > end || end - start > TimeSpan.FromDays(3660)) return Results.BadRequest(new { error = "invalid_archive_period" });
            var rows = await db.JournalEntries.AsNoTracking()
                .Where(x => x.IsPosted && x.CreatedAt >= start && x.CreatedAt <= end)
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                .GroupJoin(db.FinanceLedgerSignatures.AsNoTracking(), e => e.Id, s => s.JournalEntryId, (entry, signatures) => new { entry, signature = signatures.FirstOrDefault() })
                .Select(x => new
                {
                    x.entry.Id,
                    x.entry.EntryNumber,
                    x.entry.CreatedAt,
                    x.entry.CanonicalPayload,
                    x.entry.PreviousHash,
                    x.entry.EntryHash,
                    algorithm = x.signature == null ? null : x.signature.Algorithm,
                    keyId = x.signature == null ? null : x.signature.KeyId,
                    signature = x.signature == null ? null : x.signature.Signature,
                    publicKey = x.signature == null ? null : x.signature.PublicKey
                }).ToListAsync(ct);
            return Results.Ok(new
            {
                tenantId = Tenant(context),
                generatedAt = DateTime.UtcNow,
                entries = rows.Select(x => new
                {
                    x.Id, x.EntryNumber, x.CreatedAt, x.CanonicalPayload,
                    previousHash = x.PreviousHash is null ? null : Convert.ToBase64String(x.PreviousHash),
                    entryHash = x.EntryHash is null ? null : Convert.ToBase64String(x.EntryHash),
                    x.algorithm, x.keyId,
                    signature = x.signature is null ? null : Convert.ToBase64String(x.signature),
                    publicKey = x.publicKey is null ? null : Convert.ToBase64String(x.publicKey)
                })
            });
        });

        group.MapGet("/security/transport-key", async (HttpContext context, IHybridKemService kem, CancellationToken ct) =>
        {
            var key = await kem.GetOrCreatePublicKeyAsync(Tenant(context), ct);
            return Results.Ok(new
            {
                key.Algorithm,
                mlKemPublicKey = Convert.ToBase64String(key.MLKemPublicKey),
                x25519PublicKey = Convert.ToBase64String(key.X25519PublicKey)
            });
        });

        group.MapGet("/security/key-ceremonies", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var ceremonies = await db.FinancePqcKeyCeremonies.AsNoTracking().Include(x => x.Approvals)
                .Where(x => x.TenantId == Tenant(context)).OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.Purpose, x.Algorithm, x.RequiredApprovals, approvalCount = x.Approvals.Count, x.Status, x.CreatedAt, x.ExpiresAt, x.CompletedAt, x.ActivatedKeyId })
                .ToListAsync(ct);
            return Results.Ok(ceremonies);
        });

        group.MapPost("/security/key-ceremonies", async (HttpContext context, CreatePqcKeyCeremonyCommand command, PqcKeyCeremonyService ceremonies, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var ceremony = await ceremonies.CreateAsync(Tenant(context), UserId(context), command, ct);
            return Results.Created($"/api/finance/v2/security/key-ceremonies/{ceremony.Id:D}", ceremony);
        });

        group.MapPost("/security/key-ceremonies/{id:guid}/approve", async (Guid id, HttpContext context, PqcKeyCeremonyService ceremonies, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await ceremonies.ApproveAsync(Tenant(context), id, UserId(context), ct));
        });

        group.MapGet("/vouchers/{id:guid}/verification", async (Guid id, HttpContext context, AegisDbContext db, IPqcSignatureService pqc, CancellationToken ct) =>
        {
            var voucher = await db.Set<FinanceVoucher>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.TenantId == Tenant(context) && x.Id == id, ct);
            if (voucher is null) return Results.NotFound();
            var approval = await db.FinanceVoucherApprovals.AsNoTracking()
                .Where(x => x.TenantId == voucher.TenantId && x.FinanceVoucherId == voucher.Id && x.Decision == "APPROVED" && x.PqcSignature != null)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
            var signedPayload = approval is null ? null : FinanceVoucherSigningPayload.Create(voucher.TenantId, voucher.Id, voucher.VoucherNumber, voucher.TotalAmount, approval.ApproverUserId, approval.CreatedAt);
            var verified = signedPayload is not null && voucher.PqcAlgorithm is not null && voucher.PqcSignature is not null && voucher.PqcPublicKey is not null
                && pqc.Verify(voucher.PqcAlgorithm, signedPayload, voucher.PqcSignature, voucher.PqcPublicKey);
            return Results.Ok(new
            {
                voucher.Id,
                voucher.VoucherNumber,
                voucher.Status,
                voucher.PqcAlgorithm,
                voucher.PqcKeyId,
                Signature = voucher.PqcSignature is null ? null : Convert.ToBase64String(voucher.PqcSignature),
                PublicKey = voucher.PqcPublicKey is null ? null : Convert.ToBase64String(voucher.PqcPublicKey),
                SignedPayload = signedPayload is null ? null : Convert.ToBase64String(signedPayload),
                Verified = verified
            });
        });

        group.MapPost("/vouchers", async (HttpContext context, CreateVoucherCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var voucher = await finance.CreateVoucherAsync(Tenant(context), UserId(context), command, ct);
            return Results.Created($"/api/finance/v2/vouchers/{voucher.Id}", voucher);
        });

        group.MapPost("/vouchers/bulk-import", async (HttpContext context, BulkVoucherImportCommand command, VoucherImportService importer, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await importer.ImportAsync(Tenant(context), UserId(context), command, ct));
        });

        group.MapPost("/vouchers/{id:guid}/attachments", async (Guid id, HttpContext context, UploadVoucherAttachmentCommand command, AegisDbContext db, IR2StorageService storage, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var voucher = await db.FinanceVouchers.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == Tenant(context) && x.Id == id, ct);
            if (voucher is null) return Results.NotFound();
            if (voucher.Status == FinanceStates.Posted) return Results.UnprocessableEntity(new { error = "posted_voucher_attachments_are_immutable" });
            var fileName = Path.GetFileName(command.FileName.Trim());
            if (fileName.Length is < 1 or > 260 || !string.Equals(fileName, command.FileName.Trim(), StringComparison.Ordinal)) return Results.UnprocessableEntity(new { error = "invalid_file_name" });
            if (command.ContentType is not ("application/pdf" or "image/jpeg" or "image/png" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
                return Results.UnprocessableEntity(new { error = "unsupported_attachment_type" });
            byte[] bytes;
            try { bytes = Convert.FromBase64String(command.Base64Content); }
            catch (FormatException) { return Results.UnprocessableEntity(new { error = "invalid_attachment_encoding" }); }
            if (bytes.Length is 0 or > 20_000_000) return Results.UnprocessableEntity(new { error = "attachment_size_out_of_range" });
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var key = $"tenants/{Tenant(context):D}/finance/vouchers/{id:D}/attachments/{Guid.CreateVersion7():N}-{fileName}";
            await using var stream = new MemoryStream(bytes, writable: false);
            await storage.UploadAsync(key, stream, command.ContentType, ct);
            var attachment = new FinanceVoucherAttachment
            {
                Id = Guid.CreateVersion7(), TenantId = Tenant(context), FinanceVoucherId = id, FileName = fileName,
                ContentType = command.ContentType, StorageKey = key, Sha256 = hash, SizeBytes = bytes.Length,
                UploadedByUserId = UserId(context), UploadedAt = DateTime.UtcNow
            };
            db.FinanceVoucherAttachments.Add(attachment);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/vouchers/{id:D}/attachments/{attachment.Id:D}", attachment);
        });

        group.MapPut("/voucher-approval-rules", async (HttpContext context, ConfigureVoucherApprovalRuleCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var currency = command.Currency.Trim().ToUpperInvariant();
            var roles = command.RequiredRoles.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (!ValidCurrency(currency) || command.MinimumAmount < 0m || command.MaximumAmount <= command.MinimumAmount || roles.Length == 0 || roles.Any(x => x.Length > 100) || command.MinimumApprovals is < 1 or > 20)
                return Results.UnprocessableEntity(new { error = "invalid_approval_rule" });
            var overlaps = await db.FinanceVoucherApprovalRules.AnyAsync(x => x.TenantId == Tenant(context) && x.Currency == currency && x.IsActive
                && (command.MaximumAmount == null || x.MinimumAmount < command.MaximumAmount)
                && (x.MaximumAmount == null || command.MinimumAmount < x.MaximumAmount), ct);
            if (overlaps) return Results.Conflict(new { error = "approval_rule_range_overlaps" });
            var rule = new FinanceVoucherApprovalRule
            {
                Id = Guid.NewGuid(), TenantId = Tenant(context), Currency = currency, MinimumAmount = command.MinimumAmount,
                MaximumAmount = command.MaximumAmount, RequiredRolesJson = JsonSerializer.Serialize(roles), MinimumApprovals = command.MinimumApprovals
            };
            db.FinanceVoucherApprovalRules.Add(rule);
            await db.SaveChangesAsync(ct);
            return Results.Ok(rule);
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

        group.MapPost("/vouchers/{id:guid}/reverse", async (Guid id, HttpContext context, ReverseVoucherCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.ReverseVoucherAsync(Tenant(context), id, UserId(context), command, ct));
        });

        group.MapPost("/fee-demands", async (HttpContext context, CreateFeeDemandCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var demand = await finance.CreateFeeDemandAsync(Tenant(context), command, ct);
            // Answered as a shape rather than the entity: an installment holds a reference back to
            // its demand, so serialising the entity walked the cycle and died partway through the
            // response — a 200 with a truncated body, after the demand had already been written.
            return Results.Ok(new
            {
                demand.Id,
                demand.StudentId,
                demand.DemandNumber,
                demand.Currency,
                demand.TotalAmount,
                demand.PaidAmount,
                demand.OutstandingAmount,
                demand.Status,
                demand.IssuedAt,
                demand.DueDate,
                Installments = demand.Installments
                    .OrderBy(i => i.Sequence)
                    .Select(i => new { i.Id, i.Sequence, i.Amount, i.PaidAmount, i.DueDate, i.Status })
                    .ToList(),
            });
        });

        group.MapPut("/payment-plans/configurations", async (HttpContext context, ConfigurePaymentPlanCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.ConfigurePaymentPlanAsync(Tenant(context), command, ct));
        });

        group.MapPost("/payment-plans", async (HttpContext context, CreatePaymentPlanCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.CreatePaymentPlanAsync(Tenant(context), command, ct));
        });

        group.MapPost("/payment-plans/{id:guid}/approve", async (Guid id, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.ApprovePaymentPlanAsync(Tenant(context), id, UserId(context), ct));
        });

        group.MapPost("/security-deposits", async (HttpContext context, CreateSecurityDepositCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.CreateSecurityDepositAsync(Tenant(context), command, ct));
        });

        group.MapPost("/security-deposits/{id:guid}/request-refund", async (Guid id, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.RequestSecurityDepositRefundAsync(Tenant(context), id, ct));
        });

        group.MapPost("/security-deposits/{id:guid}/assess", async (Guid id, HttpContext context, AssessSecurityDepositCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.AssessSecurityDepositAsync(Tenant(context), id, command, ct));
        });

        group.MapPost("/security-deposits/{id:guid}/resolve", async (Guid id, HttpContext context, ResolveSecurityDepositCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.ResolveSecurityDepositAsync(Tenant(context), id, UserId(context), command, ct));
        });

        group.MapGet("/reports/security-deposits", async (string? status, Guid? studentId, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var query = db.FinanceSecurityDeposits.AsNoTracking().Include(x => x.Deductions)
                .Where(x => x.TenantId == tenantId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
            if (studentId is not null && studentId != Guid.Empty) query = query.Where(x => x.StudentId == studentId.Value);
            return Results.Ok(await query.OrderBy(x => x.CollectedAt).ToListAsync(ct));
        });

        group.MapPost("/fixed-assets/depreciation/run", async (HttpContext context, RunAssetDepreciationCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(new { periodEnd = command.PeriodEnd, lines = await finance.RunAssetDepreciationAsync(Tenant(context), UserId(context), command, ct) });
        });
        group.MapPost("/fixed-assets/{id:guid}/transfer", async (Guid id, HttpContext context, TransferFinanceAssetCommand command, FinanceAssetLifecycleService assets, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await assets.TransferAsync(Tenant(context), UserId(context), id, command, ct));
        });
        group.MapPost("/fixed-assets/{id:guid}/revalue", async (Guid id, HttpContext context, RevalueFinanceAssetCommand command, FinanceAssetLifecycleService assets, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await assets.RevalueAsync(Tenant(context), UserId(context), id, command, ct));
        });
        group.MapPost("/fixed-assets/{id:guid}/impair", async (Guid id, HttpContext context, ImpairFinanceAssetCommand command, FinanceAssetLifecycleService assets, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await assets.ImpairAsync(Tenant(context), UserId(context), id, command, ct));
        });
        group.MapPost("/fixed-assets/{id:guid}/dispose", async (Guid id, HttpContext context, DisposeFinanceAssetCommand command, FinanceAssetLifecycleService assets, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await assets.DisposeAsync(Tenant(context), UserId(context), id, command, ct));
        });
        group.MapPost("/fixed-assets/verification-cycles", async (HttpContext context, CreateAssetVerificationCycleCommand command, FinanceAssetLifecycleService assets, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await assets.CreateVerificationCycleAsync(Tenant(context), command, ct));
        });
        group.MapPost("/fixed-assets/verification-cycles/{cycleId:guid}/assets/{assetId:guid}", async (Guid cycleId, Guid assetId, HttpContext context, VerifyFinanceAssetCommand command, FinanceAssetLifecycleService assets, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await assets.VerifyAsync(Tenant(context), UserId(context), cycleId, assetId, command, ct));
        });

        group.MapPost("/payment-plans/sweep", async (DateTime? asOf, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.RunPaymentPlanSweepAsync(Tenant(context), asOf ?? DateTime.UtcNow, ct));
        });

        group.MapPost("/petty-cash/funds", async (HttpContext context, CreateFinancePettyCashFundCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var currency = command.Currency.Trim().ToUpperInvariant();
            if (command.DepartmentId == Guid.Empty || command.CustodianUserId == Guid.Empty || currency.Length != 3 || command.FloatAmount <= 0m)
                return Results.UnprocessableEntity(new { success = false, error = "Department, custodian, currency, and a positive float are required." });
            if (await db.FinancePettyCashFunds.AnyAsync(x => x.TenantId == tenantId && x.DepartmentId == command.DepartmentId, ct))
                return Results.Conflict(new { success = false, error = "This department already has a petty-cash fund." });
            var fund = new FinancePettyCashFund { Id = Guid.NewGuid(), TenantId = tenantId, DepartmentId = command.DepartmentId, CustodianUserId = command.CustodianUserId, Currency = currency, FloatAmount = command.FloatAmount, CurrentBalance = command.FloatAmount };
            db.FinancePettyCashFunds.Add(fund);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/petty-cash/funds/{fund.Id}", fund);
        });

        group.MapPost("/petty-cash/funds/{fundId:guid}/expenses", async (Guid fundId, HttpContext context, RecordFinancePettyCashExpenseCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                var tenantId = Tenant(context);
                var actorId = UserId(context);
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
                var fund = await db.FinancePettyCashFunds.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == fundId && x.IsActive, ct);
                if (fund is null) return Results.NotFound();
                if (fund.CustodianUserId != actorId && !CanManageFinance(context)) return Results.Forbid();
                if (command.Amount <= 0m || string.IsNullOrWhiteSpace(command.Category) || string.IsNullOrWhiteSpace(command.Description) || command.Amount > fund.CurrentBalance)
                    return Results.UnprocessableEntity(new { success = false, error = "Expense details are invalid or exceed the available petty-cash balance." });
                var expense = new FinancePettyCashExpense { Id = Guid.NewGuid(), TenantId = tenantId, FinancePettyCashFundId = fund.Id, Category = command.Category.Trim(), Description = command.Description.Trim(), VendorName = command.VendorName?.Trim(), Amount = decimal.Round(command.Amount, 4, MidpointRounding.AwayFromZero), ReceiptHash = command.ReceiptHash?.Trim(), SpentAt = command.SpentAt.ToUniversalTime(), CreatedByUserId = actorId };
                fund.CurrentBalance -= expense.Amount;
                fund.UpdatedAt = DateTime.UtcNow;
                db.FinancePettyCashExpenses.Add(expense);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                var threshold = fund.FloatAmount * 0.20m;
                return Results.Created($"/api/finance/v2/petty-cash/expenses/{expense.Id}", new { expense, replenishmentRequired = fund.CurrentBalance < threshold, threshold });
            });
        });

        group.MapPost("/petty-cash/funds/{fundId:guid}/counts", async (Guid fundId, HttpContext context, ConfirmFinancePettyCashCountCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var fund = await db.FinancePettyCashFunds.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == fundId && x.IsActive, ct);
            if (fund is null) return Results.NotFound();
            if (command.PhysicalBalance < 0m || command.PhysicalBalance > fund.FloatAmount)
                return Results.UnprocessableEntity(new { success = false, error = "Physical balance must be between zero and the configured float." });
            var count = new FinancePettyCashCount { Id = Guid.NewGuid(), TenantId = tenantId, FinancePettyCashFundId = fund.Id, SystemBalance = fund.CurrentBalance, PhysicalBalance = decimal.Round(command.PhysicalBalance, 4, MidpointRounding.AwayFromZero), Discrepancy = decimal.Round(command.PhysicalBalance - fund.CurrentBalance, 4, MidpointRounding.AwayFromZero), Status = command.PhysicalBalance == fund.CurrentBalance ? "MATCHED" : "DISCREPANCY", Notes = command.Notes?.Trim(), InitiatedByUserId = UserId(context), ConfirmedByUserId = UserId(context) };
            db.FinancePettyCashCounts.Add(count);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/petty-cash/counts/{count.Id}", new { count, alert = count.Status == "DISCREPANCY" });
        });

        group.MapGet("/petty-cash", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var funds = await db.FinancePettyCashFunds.AsNoTracking().Where(x => x.TenantId == tenantId).Select(x => new { x.Id, x.DepartmentId, x.CustodianUserId, x.Currency, x.FloatAmount, x.CurrentBalance, replenishmentRequired = x.CurrentBalance < x.FloatAmount * 0.20m, lastCount = db.FinancePettyCashCounts.Where(c => c.TenantId == tenantId && c.FinancePettyCashFundId == x.Id).OrderByDescending(c => c.CountedAt).Select(c => new { c.CountedAt, c.Status, c.Discrepancy }).FirstOrDefault() }).ToListAsync(ct);
            return Results.Ok(funds);
        });

        group.MapPost("/library/acquisitions", async (HttpContext context, CreateFinanceLibraryAcquisitionCommand command, AncillaryFinanceService ancillary, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var acquisition = await ancillary.CreateLibraryAcquisitionAsync(Tenant(context), UserId(context), command, ct);
            return Results.Created($"/api/finance/v2/library/acquisitions/{acquisition.Id}", acquisition);
        });

        group.MapPost("/library/fines", async (HttpContext context, CreateFinanceLibraryFineCommand command, AncillaryFinanceService ancillary, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var fine = await ancillary.CreateLibraryFineAsync(Tenant(context), command, ct);
            return Results.Created($"/api/finance/v2/library/fines/{fine.Id}", fine);
        });
        group.MapPost("/library/fines/post", async (HttpContext context, AncillaryFinanceService ancillary, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(new { posted = await ancillary.PostOpenLibraryFinesAsync(Tenant(context), UserId(context), ct) });
        });
        group.MapPost("/library/amortization/run", async (HttpContext context, RunLibraryAmortizationCommand command, AncillaryFinanceService ancillary, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(new { posted = await ancillary.RunLibraryAmortizationAsync(Tenant(context), UserId(context), command, ct) });
        });

        group.MapGet("/library/overview", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var acquisitions = await db.FinanceLibraryAcquisitions.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status == "ACTIVE").GroupBy(x => new { x.Currency, x.AccountingTreatment }).Select(x => new { x.Key.Currency, x.Key.AccountingTreatment, cost = x.Sum(y => y.Cost), carryingValue = x.Sum(y => Math.Max(0m, y.FairValue - y.AccumulatedAmortization)), count = x.Count() }).ToListAsync(ct);
            var fines = await db.FinanceLibraryFines.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status == "OPEN").GroupBy(x => x.Currency).Select(x => new { currency = x.Key, outstanding = x.Sum(y => y.Amount), count = x.Count() }).ToListAsync(ct);
            return Results.Ok(new { acquisitions, openFines = fines });
        });

        group.MapGet("/utilities/due", async (DateTime? through, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var date = (through ?? DateTime.UtcNow.AddDays(30)).ToUniversalTime();
            return Results.Ok(await db.FinanceUtilityBills.AsNoTracking().Where(x => x.TenantId == Tenant(context) && x.Status != "RECONCILED" && x.DueDate <= date).OrderBy(x => x.DueDate).Select(x => new { x.Id, x.UtilityType, x.Provider, x.AccountNumber, x.Currency, x.AmountDue, x.DueDate, x.Status, overdue = x.DueDate < DateTime.UtcNow }).ToListAsync(ct));
        });

        group.MapGet("/utilities/anomalies", async (string? utilityType, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var query = db.FinanceUtilityBills.AsNoTracking().Where(x => x.TenantId == tenantId);
            if (!string.IsNullOrWhiteSpace(utilityType)) query = query.Where(x => x.UtilityType == utilityType.Trim().ToUpperInvariant());
            var bills = await query.OrderBy(x => x.BillingPeriodStart).ToListAsync(ct);
            var anomalies = bills.GroupBy(x => new { x.AccountNumber, x.UtilityType }).SelectMany(group => group.OrderBy(x => x.BillingPeriodStart).Select((current, index) => new { current, previous = index == 0 ? null : group.ElementAt(index - 1) })).Where(x => x.previous is not null && x.previous.AmountDue > 0m && x.current.AmountDue > x.previous.AmountDue * 1.20m).Select(x => new { x.current.Id, x.current.UtilityType, x.current.Provider, x.current.AccountNumber, x.current.BillingPeriodStart, x.current.BillingPeriodEnd, x.current.AmountDue, previousAmount = x.previous!.AmountDue, increasePercent = decimal.Round((x.current.AmountDue - x.previous.AmountDue) * 100m / x.previous.AmountDue, 2, MidpointRounding.AwayFromZero) }).ToList();
            return Results.Ok(anomalies);
        });

        group.MapGet("/audit/control-monitoring", async (DateTime? from, DateTime? to, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var start = (from ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
            var end = (to ?? DateTime.UtcNow).ToUniversalTime();
            if (start >= end || (end - start).TotalDays > 366) return Results.UnprocessableEntity(new { success = false, error = "The audit period must be valid and no longer than 366 days." });
            var vouchers = await db.FinanceVouchers.AsNoTracking().Where(x => x.TenantId == tenantId && x.CreatedAt >= start && x.CreatedAt <= end).Select(x => new { x.Id, x.VoucherNumber, x.Status, x.TotalAmount, x.Currency, x.CreatedAt, x.CreatedByUserId, x.ApprovedByUserId, x.Description }).ToListAsync(ct);
            var flags = new List<object>();
            flags.AddRange(vouchers.Where(x => x.Status is ("APPROVED" or "POSTED") && x.CreatedByUserId == x.ApprovedByUserId).Select(x => new { controlId = "SOD-MAKER-CHECKER", severity = "HIGH", referenceId = x.Id, message = "Voucher maker and approver are the same user." }));
            flags.AddRange(vouchers.Where(x => x.Status == "POSTED" && x.CreatedAt.Hour is < 7 or > 21).Select(x => new { controlId = "OFF-HOURS-POSTING", severity = "MEDIUM", referenceId = x.Id, message = "Voucher was created outside the normal posting window." }));
            flags.AddRange(vouchers.Where(x => x.Status == "POSTED" && x.TotalAmount > 0m && decimal.Truncate(x.TotalAmount) == x.TotalAmount).Select(x => new { controlId = "ROUND-NUMBER", severity = "LOW", referenceId = x.Id, message = "Posted voucher has a round-number amount." }));
            flags.AddRange(vouchers.GroupBy(x => new { x.TotalAmount, x.Currency, x.Description }).Where(x => x.Key.TotalAmount > 0m && x.Count() > 1).SelectMany(x => x.Select(v => new { controlId = "DUPLICATE-PAYMENT", severity = "HIGH", referenceId = v.Id, message = "Matching amount, currency, and description appear more than once." })));
            var unbalanced = await db.JournalEntries.AsNoTracking().Where(x => x.TenantId == tenantId && x.EntryDate >= start && x.EntryDate <= end && x.TotalDebit != x.TotalCredit).Select(x => new { x.Id, x.EntryNumber, x.TotalDebit, x.TotalCredit }).ToListAsync(ct);
            flags.AddRange(unbalanced.Select(x => new { controlId = "BALANCE-CHECK", severity = "CRITICAL", referenceId = x.Id, message = $"Journal entry {x.EntryNumber} has debit {x.TotalDebit} and credit {x.TotalCredit}." }));
            return Results.Ok(new { period = new { start, end }, totalRecords = vouchers.Count, flagCount = flags.Count, flags });
        });

        group.MapGet("/vendors", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await db.FinanceVendors.AsNoTracking().Include(v => v.Contacts).Include(v => v.StandingPurchaseOrders)
                .OrderBy(v => v.LegalName).ToListAsync(ct));
        });

        group.MapPost("/vendors", async (HttpContext context, CreateFinanceVendorCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var legalName = command.LegalName.Trim();
            var tier = command.Tier.Trim().ToUpperInvariant();
            var currency = command.InvoiceCurrency.Trim().ToUpperInvariant();
            if (legalName.Length is < 2 or > 200 || tier is not ("TIER1" or "TIER2" or "TIER3") || currency.Length != 3 || !currency.All(char.IsAsciiLetter))
                return Results.UnprocessableEntity(new { success = false, error = "Vendor fields are invalid." });
            if (await db.FinanceVendors.AnyAsync(v => v.TenantId == tenantId && v.LegalName == legalName && v.Status != "INACTIVE", ct))
                return Results.Conflict(new { success = false, error = "A vendor with this legal name already exists." });
            var vendor = new FinanceVendor
            {
                Id = Guid.NewGuid(), TenantId = tenantId, LegalName = legalName, TradeName = command.TradeName?.Trim(), Tier = tier,
                TaxId = command.TaxId?.Trim(), InvoiceCurrency = currency, PaymentTerms = command.PaymentTerms.Trim().ToUpperInvariant(),
                Iban = command.Iban?.Trim(), BankName = command.BankName?.Trim(), Swift = command.Swift?.Trim(), RaastId = command.RaastId?.Trim(),
                Category = string.IsNullOrWhiteSpace(command.Category) ? "Other" : command.Category.Trim(), RelationshipNotes = command.RelationshipNotes?.Trim(),
                SanctionsStatus = "PENDING", Status = "ACTIVE"
            };
            db.FinanceVendors.Add(vendor);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/vendors/{vendor.Id}", vendor);
        });

        group.MapPost("/vendors/{id:guid}/sanctions-screen", async (Guid id, HttpContext context, VendorSanctionsService screening, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            try { return Results.Ok(new { vendorId = id, status = await screening.ScreenAsync(Tenant(context), id, ct) }); }
            catch (HttpRequestException) { return Results.Json(new { error = "sanctions_provider_unavailable" }, statusCode: StatusCodes.Status502BadGateway); }
        });

        group.MapPost("/procurement/purchase-orders", async (HttpContext context, CreateFinancePurchaseOrderCommand command, FinanceProcurementService procurement, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var order = await procurement.CreatePurchaseOrderAsync(Tenant(context), UserId(context), command, ct);
            return Results.Created($"/api/finance/v2/procurement/purchase-orders/{order.Id:D}", order);
        });

        group.MapPost("/procurement/purchase-orders/{id:guid}/approve", async (Guid id, HttpContext context, FinanceProcurementService procurement, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await procurement.ApprovePurchaseOrderAsync(Tenant(context), id, UserId(context), ct));
        });

        group.MapPost("/procurement/goods-receipts", async (HttpContext context, CreateFinanceGoodsReceiptCommand command, FinanceProcurementService procurement, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var receipt = await procurement.CreateGoodsReceiptAsync(Tenant(context), UserId(context), command, ct);
            return Results.Created($"/api/finance/v2/procurement/goods-receipts/{receipt.Id:D}", receipt);
        });

        group.MapPost("/procurement/vendor-invoices", async (HttpContext context, CreateFinanceVendorInvoiceCommand command, FinanceProcurementService procurement, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var invoice = await procurement.CreateInvoiceAsync(Tenant(context), command, ct);
            return Results.Created($"/api/finance/v2/procurement/vendor-invoices/{invoice.Id:D}", invoice);
        });

        group.MapPost("/procurement/vendor-invoices/{id:guid}/match", async (Guid id, HttpContext context, FinanceProcurementService procurement, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await procurement.MatchInvoiceAsync(Tenant(context), id, UserId(context), ct));
        });

        group.MapPost("/procurement/vendor-invoices/{id:guid}/payment-voucher", async (Guid id, HttpContext context, CreateProcurementPaymentVoucherCommand command, FinanceProcurementService procurement, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await procurement.CreatePaymentVoucherAsync(Tenant(context), id, UserId(context), command, ct));
        });

        group.MapPost("/vendors/contracts", async (HttpContext context, CreateFinanceVendorContractCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            if (command.VendorId == Guid.Empty || string.IsNullOrWhiteSpace(command.ContractNumber) || string.IsNullOrWhiteSpace(command.Title) || command.EndsOn <= command.StartsOn || command.RenewalNoticeDays is < 1 or > 365)
                return Results.UnprocessableEntity(new { error = "invalid_vendor_contract" });
            if (!await db.FinanceVendors.AnyAsync(x => x.TenantId == Tenant(context) && x.Id == command.VendorId, ct)) return Results.NotFound();
            var contract = new FinanceVendorContract { Id = Guid.NewGuid(), TenantId = Tenant(context), VendorId = command.VendorId, ContractNumber = command.ContractNumber.Trim(), Title = command.Title.Trim(), StartsOn = command.StartsOn, EndsOn = command.EndsOn, RenewalNoticeDays = command.RenewalNoticeDays };
            db.FinanceVendorContracts.Add(contract); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/vendors/contracts/{contract.Id:D}", contract);
        });

        group.MapGet("/vendors/contracts/renewals", async (int? days, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var through = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(Math.Clamp(days ?? 30, 1, 365)));
            return Results.Ok(await db.FinanceVendorContracts.AsNoTracking().Where(x => x.TenantId == Tenant(context) && x.Status == "ACTIVE" && x.EndsOn <= through).OrderBy(x => x.EndsOn).ToListAsync(ct));
        });

        group.MapPost("/vendors/{id:guid}/documents", async (Guid id, HttpContext context, UploadFinanceVendorDocumentCommand command, AegisDbContext db, IR2StorageService storage, IPqcSignatureService pqc, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            if (!await db.FinanceVendors.AnyAsync(x => x.TenantId == Tenant(context) && x.Id == id, ct)) return Results.NotFound();
            byte[] bytes;
            try { bytes = Convert.FromBase64String(command.Base64Content); } catch (FormatException) { return Results.UnprocessableEntity(new { error = "invalid_document_encoding" }); }
            var fileName = Path.GetFileName(command.FileName.Trim());
            if (bytes.Length is 0 or > 20_000_000 || fileName.Length is < 1 or > 260 || fileName != command.FileName.Trim() || command.ContentType is not ("application/pdf" or "image/jpeg" or "image/png")) return Results.UnprocessableEntity(new { error = "invalid_vendor_document" });
            var hashBytes = SHA256.HashData(bytes); var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            var signature = await pqc.SignAsync(Tenant(context), "vendor-document", hashBytes, archival: false, ct);
            var key = $"tenants/{Tenant(context):D}/finance/vendors/{id:D}/{Guid.CreateVersion7():N}-{fileName}";
            await using var stream = new MemoryStream(bytes, false); await storage.UploadAsync(key, stream, command.ContentType, ct);
            var document = new FinanceVendorDocument { Id = Guid.NewGuid(), TenantId = Tenant(context), VendorId = id, DocumentType = command.DocumentType.Trim().ToUpperInvariant(), FileName = fileName, StorageKey = key, Sha256 = hash, PqcAlgorithm = signature.Algorithm, PqcKeyId = signature.KeyId, PqcSignature = signature.Signature };
            db.FinanceVendorDocuments.Add(document); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/vendors/{id:D}/documents/{document.Id:D}", new { document.Id, document.DocumentType, document.FileName, document.Sha256, document.PqcAlgorithm, document.PqcKeyId });
        });

        group.MapPost("/standing-purchase-orders", async (HttpContext context, CreateStandingPurchaseOrderCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            if (command.VendorId == Guid.Empty || command.LimitAmount <= 0m || command.StartsOn > command.EndsOn || string.IsNullOrWhiteSpace(command.Number))
                return Results.UnprocessableEntity(new { success = false, error = "Standing purchase order fields are invalid." });
            var vendor = await db.FinanceVendors.SingleOrDefaultAsync(v => v.TenantId == tenantId && v.Id == command.VendorId, ct);
            if (vendor is null) return Results.NotFound(new { success = false, error = "Vendor was not found." });
            if (vendor.Status != "ACTIVE" || vendor.SanctionsStatus != "CLEAR") return Results.UnprocessableEntity(new { success = false, error = "Vendor must be active and sanctions-cleared before payment commitments." });
            if (await db.FinanceStandingPurchaseOrders.AnyAsync(p => p.TenantId == tenantId && p.Number == command.Number.Trim(), ct)) return Results.Conflict(new { success = false, error = "Standing purchase order number already exists." });
            var order = new FinanceStandingPurchaseOrder { Id = Guid.NewGuid(), TenantId = tenantId, VendorId = vendor.Id, Number = command.Number.Trim(), Period = command.Period.Trim().ToUpperInvariant(), LimitAmount = command.LimitAmount, StartsOn = command.StartsOn, EndsOn = command.EndsOn };
            db.FinanceStandingPurchaseOrders.Add(order);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/standing-purchase-orders/{order.Id}", order);
        });

        group.MapPost("/standing-purchase-orders/spend", async (HttpContext context, RecordStandingPurchaseOrderSpendCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                if (!CanManageFinance(context)) return Results.Forbid();
                if (command.StandingPurchaseOrderId == Guid.Empty || command.Amount <= 0m || string.IsNullOrWhiteSpace(command.Description))
                    return Results.UnprocessableEntity(new { success = false, error = "Spend fields are invalid." });
                await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
                var order = await db.FinanceStandingPurchaseOrders.SingleOrDefaultAsync(p => p.TenantId == Tenant(context) && p.Id == command.StandingPurchaseOrderId, ct);
                if (order is null) return Results.NotFound();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (order.Status != "ACTIVE" || today < order.StartsOn || today > order.EndsOn) return Results.UnprocessableEntity(new { success = false, error = "Standing purchase order is not active for the current date." });
                if (order.UsedAmount + command.Amount > order.LimitAmount) return Results.UnprocessableEntity(new { success = false, error = "Standing purchase order limit would be exceeded." });
                order.UsedAmount += command.Amount;
                var spend = new FinanceStandingPurchaseOrderSpend { Id = Guid.NewGuid(), TenantId = Tenant(context), StandingPurchaseOrderId = order.Id, Amount = command.Amount, Description = command.Description.Trim() };
                db.FinanceStandingPurchaseOrderSpend.Add(spend);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return Results.Ok(new { spendId = spend.Id, orderId = order.Id, order.UsedAmount, order.LimitAmount, remaining = order.LimitAmount - order.UsedAmount });
            });
        });

        group.MapPost("/exchange-rates/snapshots", async (HttpContext context, StoreExchangeRateCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var baseCurrency = command.BaseCurrency.Trim().ToUpperInvariant();
            var quoteCurrency = command.QuoteCurrency.Trim().ToUpperInvariant();
            var source = command.Source.Trim().ToUpperInvariant();
            if (!ValidCurrency(baseCurrency) || !ValidCurrency(quoteCurrency) || baseCurrency == quoteCurrency || command.Rate <= 0m || command.ValidForDate == default || source is not ("OER" or "ECB" or "SBP" or "MANUAL"))
                return Results.UnprocessableEntity(new { success = false, error = "Exchange-rate fields are invalid." });
            if (!await HasFcyExposureAsync(db, tenantId, baseCurrency, ct))
                return Results.UnprocessableEntity(new { success = false, error = "FCY rate access is disabled because this tenant has no foreign-currency exposure." });
            var validDate = command.ValidForDate.Date;
            var snapshot = await db.ExchangeRateSnapshots.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.BaseCurrency == baseCurrency && x.QuoteCurrency == quoteCurrency && x.ValidForDate == validDate, ct);
            if (snapshot is null)
            {
                snapshot = new ExchangeRateSnapshot { Id = Guid.NewGuid(), TenantId = tenantId, BaseCurrency = baseCurrency, QuoteCurrency = quoteCurrency, ValidForDate = validDate };
                db.ExchangeRateSnapshots.Add(snapshot);
            }
            snapshot.Rate = command.Rate;
            snapshot.Source = source;
            snapshot.PulledAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(snapshot);
        });

        group.MapGet("/exchange-rates/{baseCurrency}/{quoteCurrency}", async (string baseCurrency, string quoteCurrency, DateTime? asOf, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var baseCode = baseCurrency.Trim().ToUpperInvariant();
            var quoteCode = quoteCurrency.Trim().ToUpperInvariant();
            if (!ValidCurrency(baseCode) || !ValidCurrency(quoteCode) || baseCode == quoteCode) return Results.UnprocessableEntity(new { success = false, error = "Currency pair is invalid." });
            if (!await HasFcyExposureAsync(db, tenantId, baseCode, ct)) return Results.NoContent();
            var date = (asOf ?? DateTime.UtcNow).Date;
            var snapshot = await db.ExchangeRateSnapshots.AsNoTracking().Where(x => x.TenantId == tenantId && x.BaseCurrency == baseCode && x.QuoteCurrency == quoteCode && x.ValidForDate <= date).OrderByDescending(x => x.ValidForDate).FirstOrDefaultAsync(ct);
            return snapshot is null ? Results.NotFound(new { success = false, error = "No historical exchange rate exists for this pair." }) : Results.Ok(snapshot);
        });

        group.MapPost("/foreign-currency/transactions", async (HttpContext context, RecordFinanceForeignCurrencyTransactionCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var sourceType = command.SourceType.Trim().ToUpperInvariant();
            var foreignCurrency = command.ForeignCurrency.Trim().ToUpperInvariant();
            var configuration = await db.TenantFinanceConfigurations.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
            if (configuration is null) return Results.UnprocessableEntity(new { success = false, error = "Tenant finance configuration is required." });
            var functionalCurrency = configuration.BaseCurrency.Trim().ToUpperInvariant();
            if (sourceType is not ("STUDENT_FEE" or "VENDOR" or "GRANT" or "PAYROLL" or "SWIFT" or "INVESTMENT") || command.SourceId == Guid.Empty || !ValidCurrency(foreignCurrency) || !ValidCurrency(functionalCurrency) || foreignCurrency == functionalCurrency || command.ForeignAmount <= 0m || command.FunctionalAmount <= 0m || command.RateUsed <= 0m || command.RateDate == default || command.RateSource.Trim().ToUpperInvariant() is not ("OER" or "ECB" or "SBP" or "MANUAL"))
                return Results.UnprocessableEntity(new { success = false, error = "Foreign-currency transaction fields are invalid." });
            var sourceCurrency = sourceType switch
            {
                "STUDENT_FEE" => await db.FeeDemands.AsNoTracking().Where(x => x.TenantId == tenantId && x.Id == command.SourceId).Select(x => x.Currency).SingleOrDefaultAsync(ct),
                "VENDOR" => await db.FinanceVendors.AsNoTracking().Where(x => x.TenantId == tenantId && x.Id == command.SourceId).Select(x => x.InvoiceCurrency).SingleOrDefaultAsync(ct),
                "GRANT" => await db.FinanceGrants.AsNoTracking().Where(x => x.TenantId == tenantId && x.Id == command.SourceId).Select(x => x.Currency).SingleOrDefaultAsync(ct),
                "INVESTMENT" => await db.FinanceInvestmentHoldings.AsNoTracking().Where(x => x.TenantId == tenantId && x.Id == command.SourceId).Select(x => x.Currency).SingleOrDefaultAsync(ct),
                _ => foreignCurrency
            };
            if (sourceCurrency is null || !string.Equals(sourceCurrency.Trim(), foreignCurrency, StringComparison.OrdinalIgnoreCase))
                return Results.UnprocessableEntity(new { success = false, error = "The source record does not have the requested foreign currency." });
            var transaction = new FinanceForeignCurrencyTransaction
            {
                Id = Guid.CreateVersion7(), TenantId = tenantId, SourceType = sourceType, SourceId = command.SourceId,
                ForeignAmount = command.ForeignAmount, ForeignCurrency = foreignCurrency, FunctionalAmount = command.FunctionalAmount,
                FunctionalCurrency = functionalCurrency, RateUsed = command.RateUsed, RateSource = command.RateSource.Trim().ToUpperInvariant(), RateDate = command.RateDate.ToUniversalTime()
            };
            db.FinanceForeignCurrencyTransactions.Add(transaction);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/foreign-currency/transactions/{transaction.Id}", transaction);
        });

        group.MapGet("/foreign-currency/exposure", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var rows = await db.FinanceForeignCurrencyTransactions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && !x.IsSettled)
                .GroupBy(x => new { x.ForeignCurrency, x.FunctionalCurrency })
                .Select(x => new { x.Key.ForeignCurrency, x.Key.FunctionalCurrency, ForeignAmount = x.Sum(y => y.ForeignAmount), FunctionalAmount = x.Sum(y => y.FunctionalAmount) })
                .OrderBy(x => x.ForeignCurrency).ToListAsync(ct);
            return Results.Ok(rows);
        });

        group.MapPost("/foreign-currency/transactions/{id:guid}/settle", async (Guid id, HttpContext context, SettleFinanceForeignCurrencyTransactionCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            if (command.SettlementRate <= 0m || command.SettledAt == default) return Results.UnprocessableEntity(new { success = false, error = "Settlement rate and date are required." });
            var transaction = await db.FinanceForeignCurrencyTransactions.SingleOrDefaultAsync(x => x.TenantId == Tenant(context) && x.Id == id, ct);
            if (transaction is null) return Results.NotFound();
            if (transaction.IsSettled) return Results.UnprocessableEntity(new { success = false, error = "Foreign-currency transaction is already settled." });
            transaction.RealizedFxGainLoss = Math.Round(transaction.ForeignAmount * command.SettlementRate - transaction.FunctionalAmount, 4, MidpointRounding.AwayFromZero);
            transaction.IsSettled = true;
            transaction.SettledAt = command.SettledAt.ToUniversalTime();
            await db.SaveChangesAsync(ct);
            return Results.Ok(transaction);
        });

        group.MapPost("/payments", async (HttpContext context, RecordFinancePaymentCommand command, IFinanceModuleService finance, CancellationToken ct) =>
            Results.Ok(await finance.RecordPaymentAsync(Tenant(context), command, ct)));

        group.MapPost("/payments/{gateway}/request-to-pay", async (string gateway, HttpContext context, InitiateFinanceRtpCommand command, AegisDbContext db, PaymentGatewayRouter router, CancellationToken ct) =>
        {
            var tenantId = Tenant(context);
            var normalizedGateway = gateway.Trim().ToLowerInvariant();
            if (normalizedGateway.Length is < 2 or > 40 || normalizedGateway.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-')))
                return Results.UnprocessableEntity(new { success = false, error = "Gateway is invalid." });
            if (command.FeeDemandId == Guid.Empty || !ValidCurrency(command.Currency))
                return Results.UnprocessableEntity(new { success = false, error = "Request-to-pay fields are invalid." });
            var demand = await db.FeeDemands.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.FeeDemandId, ct);
            if (demand is null) return Results.NotFound(new { success = false, error = "Fee demand was not found." });
            var payerReference = command.PayerReference.Trim();
            if (!CanManageFinance(context))
            {
                if (StudentId(context) != demand.StudentId) return Results.Forbid();
            }
            if (!CanManageFinance(context) || payerReference.Equals("registered", StringComparison.OrdinalIgnoreCase))
            {
                var profileJson = await db.StudentProfiles.AsNoTracking().Where(x => x.TenantId == tenantId && x.StudentId == demand.StudentId).Select(x => x.ProfileJson).SingleOrDefaultAsync(ct);
                payerReference = StudentPaymentAlias(profileJson);
                if (string.IsNullOrWhiteSpace(payerReference))
                    return Results.UnprocessableEntity(new { success = false, error = "A registered mobile number or payment alias is required." });
            }
            if (payerReference.Length is < 3 or > 200) return Results.UnprocessableEntity(new { success = false, error = "Payer reference is invalid." });
            if (!string.Equals(demand.Currency, command.Currency.Trim().ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
                return Results.UnprocessableEntity(new { success = false, error = "Payment currency does not match the fee demand." });
            if (demand.OutstandingAmount <= 0m) return Results.UnprocessableEntity(new { success = false, error = "Fee demand is already settled." });
            GatewayRtpResult result;
            try
            {
                result = await router.SendRtpAsync(tenantId, normalizedGateway,
                    new GatewayRtpRequest(demand.Id, command.FeeInstallmentId, demand.OutstandingAmount, demand.Currency,
                        payerReference, Guid.CreateVersion7().ToString("D")), ct);
            }
            catch (InvalidOperationException) { return Results.UnprocessableEntity(new { success = false, error = "Payment gateway request could not be initiated." }); }
            catch (NotSupportedException) { return Results.UnprocessableEntity(new { success = false, error = "The selected live payment gateway is not supported." }); }
            catch (HttpRequestException) { return Results.Json(new { success = false, error = "Payment gateway is temporarily unavailable." }, statusCode: StatusCodes.Status502BadGateway); }
            var eventRecord = new PaymentGatewayEvent
            {
                Id = Guid.CreateVersion7(), TenantId = tenantId, GatewaySlug = normalizedGateway, EventType = "RTP_SENT", ReferenceId = result.ReferenceId,
                Amount = demand.OutstandingAmount, Currency = demand.Currency, Status = "PENDING", IsSandbox = result.IsSandbox,
                SanitizedPayloadJson = result.SanitizedResponseJson
            };
            db.PaymentGatewayEvents.Add(eventRecord);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { success = true, isSandbox = eventRecord.IsSandbox, rtpRequestId = result.ReferenceId, amount = demand.OutstandingAmount, currency = demand.Currency, result.ExpiresAt, result.DeepLink });
        });

        group.MapPost("/payroll/import-and-post", async (HttpContext context, ImportFinancePayrollCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.ImportAndPostPayrollAsync(Tenant(context), UserId(context), command, ct));
        });

        group.MapPut("/payroll/bank-instructions", async (HttpContext context, ConfigurePayrollBankInstructionCommand command, FinancePayrollSettlementService payroll, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var instruction = await payroll.ConfigureInstructionAsync(Tenant(context), command, ct);
            return Results.Ok(new { instruction.Id, instruction.EmployeeId, instruction.RoutingCode, instruction.AccountName, instruction.Currency, instruction.IsActive, instruction.UpdatedAt });
        });

        group.MapPost("/payroll/{postingId:guid}/payment-files", async (Guid postingId, HttpContext context, GeneratePayrollPaymentFileCommand command, FinancePayrollSettlementService payroll, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var artifact = await payroll.GeneratePaymentFileAsync(Tenant(context), postingId, UserId(context), command, ct);
            return Results.Ok(new { artifact.Id, artifact.PayrollPostingId, artifact.Format, artifact.Sha256, artifact.PqcAlgorithm, artifact.PqcKeyId, artifact.TotalAmount, artifact.RecordCount, artifact.GeneratedAt });
        });

        group.MapPut("/payroll/gratuity/assumptions", async (HttpContext context, ConfigureGratuityAssumptionCommand command, FinancePayrollSettlementService payroll, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await payroll.ConfigureAssumptionAsync(Tenant(context), command, ct));
        });

        group.MapPost("/payroll/gratuity/valuations", async (DateTime valuationDate, HttpContext context, FinancePayrollSettlementService payroll, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await payroll.RunValuationAsync(Tenant(context), UserId(context), valuationDate, ct));
        });

        group.MapPost("/bank-accounts", async (HttpContext context, CreateFinanceBankAccountCommand command, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var currency = command.Currency.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 160 || string.IsNullOrWhiteSpace(command.AccountReference) || command.AccountReference.Length > 34 || !ValidCurrency(currency) || command.ChartOfAccountId == Guid.Empty)
                return Results.UnprocessableEntity(new { success = false, error = "Bank account fields are invalid." });
            var account = new FinanceBankAccount { Id = Guid.NewGuid(), TenantId = Tenant(context), Name = command.Name.Trim(), AccountReference = command.AccountReference.Trim(), Currency = currency, ChartOfAccountId = command.ChartOfAccountId };
            db.FinanceBankAccounts.Add(account);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/finance/v2/bank-accounts/{account.Id}", account);
        });

        group.MapPost("/bank-feeds/import", async (HttpContext context, ImportFinanceBankFeedCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.ImportBankFeedAsync(Tenant(context), command, ct));
        });

        group.MapGet("/bank-feeds/unmatched", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await db.FinanceBankFeedTransactions.AsNoTracking().Where(x => x.TenantId == Tenant(context) && x.Status == "UNMATCHED").OrderBy(x => x.BookingDate).ToListAsync(ct));
        });

        group.MapGet("/bank-feeds/{id:guid}/matches", async (Guid id, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var feed = await db.FinanceBankFeedTransactions.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.Status == "UNMATCHED", ct);
            if (feed is null) return Results.NotFound();
            var from = feed.BookingDate.AddDays(-2);
            var to = feed.BookingDate.AddDays(2);
            var journals = await db.JournalEntries.AsNoTracking().Where(x => x.TenantId == tenantId && x.IsPosted && x.EntryDate >= from && x.EntryDate <= to && (x.TotalDebit == Math.Abs(feed.Amount) || x.TotalCredit == Math.Abs(feed.Amount))).Select(x => new { x.Id, x.EntryNumber, x.EntryDate, x.Description, x.TotalDebit, x.TotalCredit }).ToListAsync(ct);
            var tokens = feed.Narration.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(x => x.Length >= 3).Select(x => x.ToUpperInvariant()).Distinct().ToArray();
            var matches = journals.Select(j => new { journal = j, score = tokens.Length == 0 ? 0 : tokens.Count(t => (j.Description ?? string.Empty).ToUpperInvariant().Contains(t)) * 100 / tokens.Length }).OrderByDescending(x => x.score).ThenBy(x => Math.Abs((x.journal.EntryDate - feed.BookingDate).TotalDays)).Select(x => new { x.journal, x.score }).Take(10);
            return Results.Ok(new { feed.Id, candidates = matches });
        });

        group.MapPost("/bank-feeds/{id:guid}/match/{journalId:guid}", async (Guid id, Guid journalId, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var feed = await db.FinanceBankFeedTransactions.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.Status == "UNMATCHED", ct);
            var journal = await db.JournalEntries.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == journalId && x.IsPosted, ct);
            if (feed is null || journal is null) return Results.NotFound();
            if (Math.Abs(feed.Amount) != journal.TotalDebit && Math.Abs(feed.Amount) != journal.TotalCredit || Math.Abs((journal.EntryDate - feed.BookingDate).TotalDays) > 2)
                return Results.UnprocessableEntity(new { success = false, error = "The journal entry does not satisfy amount and date reconciliation rules." });
            feed.Status = "MATCHED";
            feed.MatchedJournalEntryId = journal.Id;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { feed.Id, feed.Status, feed.MatchedJournalEntryId });
        });
        group.MapPost("/bank-feeds/{id:guid}/post-adjustment", async (Guid id, HttpContext context, PostBankReconciliationAdjustmentCommand command, FinanceTreasuryService treasury, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await treasury.PostReconciliationAdjustmentAsync(Tenant(context), UserId(context), id, command, ct));
        });
        group.MapGet("/treasury/forecast", async (DateTime? asOf, HttpContext context, FinanceTreasuryService treasury, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await treasury.ForecastAsync(Tenant(context), asOf ?? DateTime.UtcNow, ct));
        });

        group.MapPost("/grants", async (HttpContext context, CreateFinanceGrantCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Created("/api/finance/v2/grants", await finance.CreateGrantAsync(Tenant(context), command, ct));
        });
        group.MapPost("/grants/receipt", async (HttpContext context, RecordFinanceGrantReceiptCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.RecordGrantReceiptAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapPost("/scholarships/awards", async (HttpContext context, AwardFinanceScholarshipCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.AwardScholarshipAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapPost("/scholarships/programs", async (HttpContext context, CreateScholarshipProgramCommand command, FinanceScholarshipService scholarships, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var program = await scholarships.CreateProgramAsync(Tenant(context), UserId(context), command, ct);
            return Results.Created($"/api/finance/v2/scholarships/programs/{program.Id:D}", program);
        });
        group.MapPost("/scholarships/applications", async (HttpContext context, ApplyForScholarshipCommand command, FinanceScholarshipService scholarships, CancellationToken ct) =>
        {
            var studentId = UserId(context);
            if (!CanManageFinance(context) && command.StudentId != studentId) return Results.Forbid();
            var application = await scholarships.ApplyAsync(Tenant(context), command, ct);
            return Results.Created($"/api/finance/v2/scholarships/applications/{application.Id:D}", application);
        });
        group.MapPost("/scholarships/applications/{id:guid}/transition", async (Guid id, HttpContext context, TransitionScholarshipApplicationCommand command, FinanceScholarshipService scholarships, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await scholarships.TransitionAsync(Tenant(context), UserId(context), id, command, ct));
        });
        group.MapPost("/scholarships/applications/{id:guid}/disburse", async (Guid id, HttpContext context, FinanceScholarshipService scholarships, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await scholarships.DisburseAsync(Tenant(context), UserId(context), id, ct));
        });
        group.MapGet("/grants/{id:guid}/donor-report", async (Guid id, HttpContext context, FinanceScholarshipService scholarships, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await scholarships.DonorReportAsync(Tenant(context), id, ct));
        });

        group.MapPost("/utilities/bills", async (HttpContext context, CreateFinanceUtilityBillCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Created("/api/finance/v2/utilities/bills", await finance.CreateUtilityBillAsync(Tenant(context), command, ct));
        });
        group.MapPost("/utilities/bills/{id:guid}/approve", async (Guid id, HttpContext context, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.ApproveUtilityBillAsync(Tenant(context), UserId(context), id, ct));
        });
        group.MapGet("/utilities/bills/due", async (DateTime? until, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var date = until ?? DateTime.UtcNow.AddDays(7);
            return Results.Ok(await db.FinanceUtilityBills.AsNoTracking().Where(x => x.TenantId == Tenant(context) && x.Status != "PAID" && x.DueDate <= date).OrderBy(x => x.DueDate).ToListAsync(ct));
        });

        group.MapPost("/insurance/policies", async (HttpContext context, CreateFinanceInsurancePolicyCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Created("/api/finance/v2/insurance/policies", await finance.CreateInsurancePolicyAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapPost("/insurance/claims", async (HttpContext context, CreateFinanceInsuranceClaimCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Created("/api/finance/v2/insurance/claims", await finance.CreateInsuranceClaimAsync(Tenant(context), command, ct));
        });
        group.MapPost("/insurance/claims/settle", async (HttpContext context, SettleFinanceInsuranceClaimCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.SettleInsuranceClaimAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapPost("/insurance/amortize", async (HttpContext context, RunInsuranceAmortizationCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(new { postedMonths = await finance.RunInsuranceAmortizationAsync(Tenant(context), UserId(context), command, ct) });
        });

        group.MapPost("/inventory/items", async (HttpContext context, CreateFinanceInventoryItemCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Created("/api/finance/v2/inventory/items", await finance.CreateInventoryItemAsync(Tenant(context), command, ct));
        });
        group.MapPost("/inventory/receipts", async (HttpContext context, ReceiveFinanceInventoryCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.ReceiveInventoryAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapPost("/inventory/issues", async (HttpContext context, IssueFinanceInventoryCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.IssueInventoryAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapPost("/inventory/counts", async (HttpContext context, CountFinanceInventoryCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.CountInventoryAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapGet("/inventory/low-stock", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await db.FinanceInventoryItems.AsNoTracking().Where(x => x.TenantId == Tenant(context) && x.Status == "ACTIVE" && x.OnHandQuantity <= x.ReorderLevel).OrderBy(x => x.ItemCode).ToListAsync(ct));
        });

        group.MapPost("/fleet/costs", async (HttpContext context, PostFinanceFleetCostCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.PostFleetCostAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapPost("/fleet/trips", async (HttpContext context, RecordFinanceFleetTripCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Created("/api/finance/v2/fleet/trips", await finance.RecordFleetTripAsync(Tenant(context), command, ct));
        });
        group.MapPost("/fleet/profiles", async (HttpContext context, RegisterFinanceFleetProfileCommand command, AncillaryFinanceService ancillary, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var profile = await ancillary.RegisterFleetAsync(Tenant(context), UserId(context), command, ct);
            return Results.Created($"/api/finance/v2/fleet/profiles/{profile.Id:D}", profile);
        });
        group.MapPost("/fleet/{vehicleId:guid}/maintenance", async (Guid vehicleId, HttpContext context, ScheduleFleetMaintenanceCommand command, AncillaryFinanceService ancillary, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await ancillary.ScheduleMaintenanceAsync(Tenant(context), vehicleId, command, ct));
        });
        group.MapPost("/fleet/maintenance/{id:guid}/complete", async (Guid id, HttpContext context, CompleteFleetMaintenanceCommand command, AncillaryFinanceService ancillary, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await ancillary.CompleteMaintenanceAsync(Tenant(context), UserId(context), id, command, ct));
        });
        group.MapPost("/fleet/{vehicleId:guid}/dispose", async (Guid vehicleId, HttpContext context, DisposeFinanceAssetCommand command, AncillaryFinanceService ancillary, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await ancillary.DisposeFleetAsync(Tenant(context), UserId(context), vehicleId, command, ct));
        });
        group.MapGet("/fleet/summary", async (DateTime? from, DateTime? to, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var start = from ?? DateTime.UtcNow.AddMonths(-1);
            var end = to ?? DateTime.UtcNow;
            var costs = await db.FinanceFleetCostPostings.AsNoTracking().Where(x => x.TenantId == tenantId && x.CostDate >= start && x.CostDate <= end).GroupBy(x => x.CostType).Select(x => new { costType = x.Key, amount = x.Sum(y => y.Amount), litres = x.Sum(y => y.Litres) }).ToListAsync(ct);
            var distance = await db.FinanceFleetTrips.AsNoTracking().Where(x => x.TenantId == tenantId && x.TripDate >= start && x.TripDate <= end).SumAsync(x => (decimal?)x.DistanceKm, ct) ?? 0m;
            return Results.Ok(new { from = start, to = end, costs, distanceKm = distance });
        });

        group.MapPost("/investments/funds", async (HttpContext context, CreateFinanceInvestmentFundCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Created("/api/finance/v2/investments/funds", await finance.CreateInvestmentFundAsync(Tenant(context), command, ct));
        });
        group.MapPost("/investments/holdings", async (HttpContext context, CreateFinanceInvestmentHoldingCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Created("/api/finance/v2/investments/holdings", await finance.CreateInvestmentHoldingAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapPost("/investments/valuation", async (HttpContext context, RecordInvestmentValuationCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.RecordInvestmentValuationAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapPost("/investments/interest", async (HttpContext context, RecordInvestmentInterestCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.RecordInvestmentInterestAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapGet("/investments/overview", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var holdings = await db.FinanceInvestmentHoldings.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status == "HELD").Select(x => new { x.Id, x.InstrumentType, x.Issuer, x.AssetClass, x.Currency, x.CostBasis, x.MarketValue, x.MaturityDate, x.AnnualYieldPercent }).ToListAsync(ct);
            var total = holdings.Sum(x => x.MarketValue);
            var concentration = holdings.GroupBy(x => x.Issuer).Select(x => new { issuer = x.Key, marketValue = x.Sum(y => y.MarketValue), percent = total == 0m ? 0m : Math.Round(x.Sum(y => y.MarketValue) / total * 100m, 4), exceedsTwentyPercent = total > 0m && x.Sum(y => y.MarketValue) / total > 0.20m }).OrderByDescending(x => x.percent);
            var funds = await db.FinanceInvestmentFunds.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status == "ACTIVE").Select(x => new { x.Name, x.FundType, x.Currency, x.CurrentValue, x.SpendingRatePercent, spendable = Math.Round(x.CurrentValue * x.SpendingRatePercent / 100m, 4), reserveShortfall = x.FundType == "OPERATING_RESERVE" && x.CurrentValue < x.ReserveTarget ? x.ReserveTarget - x.CurrentValue : 0m }).ToListAsync(ct);
            return Results.Ok(new { totalMarketValue = total, holdings, concentration, funds, maturity = holdings.Where(x => x.MaturityDate.HasValue).OrderBy(x => x.MaturityDate) });
        });
        group.MapPost("/investments/benchmarks", async (HttpContext context, RecordInvestmentBenchmarkCommand command, FinanceTreasuryService treasury, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await treasury.RecordBenchmarkAsync(Tenant(context), command, ct));
        });
        group.MapGet("/investments/maturity-ladder", async (DateTime from, DateTime to, HttpContext context, FinanceTreasuryService treasury, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await treasury.MaturityLadderAsync(Tenant(context), from, to, ct));
        });
        group.MapGet("/investments/benchmark-performance", async (HttpContext context, FinanceTreasuryService treasury, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await treasury.PerformanceAgainstBenchmarksAsync(Tenant(context), ct));
        });

        group.MapPost("/tax/rules", async (HttpContext context, CreateFinanceTaxRuleCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Created("/api/finance/v2/tax/rules", await finance.CreateTaxRuleAsync(Tenant(context), command, ct));
        });
        group.MapPost("/tax/withholding", async (HttpContext context, RecordFinanceWithholdingCommand command, IFinanceModuleService finance, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            return Results.Ok(await finance.RecordWithholdingAsync(Tenant(context), UserId(context), command, ct));
        });
        group.MapGet("/tax/filing-summary", async (string filingPeriod, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            if (!System.Text.RegularExpressions.Regex.IsMatch(filingPeriod, "^\\d{4}-(0[1-9]|1[0-2])$")) return Results.UnprocessableEntity(new { success = false, error = "Filing period must be yyyy-MM." });
            var records = await db.FinanceWithholdingRecords.AsNoTracking().Where(x => x.TenantId == Tenant(context) && x.FilingPeriod == filingPeriod).GroupBy(x => new { x.TaxType, x.FilerStatus }).Select(x => new { x.Key.TaxType, x.Key.FilerStatus, gross = x.Sum(y => y.GrossAmount), withheld = x.Sum(y => y.WithheldAmount), count = x.Count() }).ToListAsync(ct);
            return Results.Ok(new { filingPeriod, records });
        });
        group.MapGet("/tax/zakat", async (decimal nisab, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            if (nisab <= 0m) return Results.UnprocessableEntity(new { success = false, error = "Nisab must be positive." });
            var tenantId = Tenant(context);
            var assets = await db.FinanceInvestmentHoldings.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status == "HELD" && x.MarketValue > 0m).SumAsync(x => (decimal?)x.MarketValue, ct) ?? 0m;
            var zakatDue = assets >= nisab ? Math.Round(assets * 0.025m, 4, MidpointRounding.AwayFromZero) : 0m;
            return Results.Ok(new { nisab, zakatableInvestmentAssets = assets, nisabReached = assets >= nisab, zakatRatePercent = 2.5m, zakatDue });
        });

        group.MapGet("/gateways", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var configurations = await db.PaymentGatewayConfigurations.AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.GatewaySlug)
                .Select(x => new
                {
                    x.GatewaySlug, x.Environment, x.IsActive, x.HealthStatus,
                    x.LastHealthCheckAt, x.HealthError, x.ConfiguredAt
                })
                .ToListAsync(ct);
            return Results.Ok(configurations);
        });

        group.MapPut("/gateways/{slug}", async (string slug, HttpContext context, JsonElement credentials, AegisDbContext db, WizardGatewayService gateways, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var license = await db.Licenses.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
            if (license is null || license.Status != LicenseStatus.Active || (license.ExpiresAt is not null && license.ExpiresAt <= DateTime.UtcNow))
                return Results.UnprocessableEntity(new { success = false, error = "Live payment gateways require an active licensed instance." });
            try
            {
                var result = await gateways.ConfigureAsync(tenantId, slug, credentials, ct, UserId(context));
                return Results.Ok(result);
            }
            catch (ArgumentException ex) { return Results.UnprocessableEntity(new { success = false, error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.UnprocessableEntity(new { success = false, error = ex.Message }); }
        });

        group.MapPost("/gateways/{slug}/health-check", async (string slug, HttpContext context, AegisDbContext db, WizardGatewayService gateways, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var license = await db.Licenses.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
            if (license is null || license.Status != LicenseStatus.Active || (license.ExpiresAt is not null && license.ExpiresAt <= DateTime.UtcNow))
                return Results.UnprocessableEntity(new { success = false, error = "Live payment gateways require an active licensed instance." });
            try { return Results.Ok(await gateways.HealthCheckAsync(tenantId, slug, ct)); }
            catch (ArgumentException ex) { return Results.UnprocessableEntity(new { success = false, error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.UnprocessableEntity(new { success = false, error = ex.Message }); }
        });

        group.MapGet("/gateways/{slug}/events", async (string slug, string? status, string? eventType, HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            var gateway = slug.Trim().ToLowerInvariant();
            if (gateway.Length is < 2 or > 40 || gateway.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-'))) return Results.UnprocessableEntity(new { success = false, error = "Gateway slug is invalid." });
            var query = db.PaymentGatewayEvents.AsNoTracking().Where(x => x.TenantId == Tenant(context) && x.GatewaySlug == gateway);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
            if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(x => x.EventType == eventType.Trim().ToUpperInvariant());
            var events = await query.OrderByDescending(x => x.CreatedAt).Take(50)
                .Select(x => new { x.Id, x.GatewaySlug, x.EventType, x.ReferenceId, x.FinancePaymentId, x.Amount, x.Currency, x.Status, x.IsSandbox, x.ErrorMessage, x.CreatedAt })
                .ToListAsync(ct);
            return Results.Ok(events);
        });

        group.MapGet("/audit/dashboard", async (HttpContext context, AegisDbContext db, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            if (!CanAuditFinance(context)) return Results.Forbid();
            var tenantId = Tenant(context);
            var runs = await db.FinanceControlEvaluationBatches.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.EvaluatedAt).Take(100).ToListAsync(ct);
            var engagements = await db.FinanceAuditorEngagements.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.AuditorName, x.AuditorEmail, x.ScopesJson, x.CreatedAt, x.ExpiresAt, x.RevokedAt }).ToListAsync(ct);
            var packages = await db.FinanceAuditEvidencePackages.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.GeneratedAt).Take(50)
                .Select(x => new { x.Id, x.Framework, x.PeriodFrom, x.PeriodTo, x.ContentHash, x.PqcAlgorithm, x.PqcKeyId, x.GeneratedAt }).ToListAsync(ct);
            return Results.Ok(new
            {
                controlCount = controls.GetLibrary().Count,
                latestRun = runs.FirstOrDefault(),
                runs,
                activeEngagements = engagements.Count(x => x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow),
                engagements,
                packages
            });
        });

        group.MapGet("/audit/control-library", (HttpContext context, FinanceContinuousControlService controls) =>
            CanAuditFinance(context) ? Results.Ok(controls.GetLibrary()) : Results.Forbid());

        group.MapPost("/audit/engagements", async (HttpContext context, CreateAuditorEngagementCommand command, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            try
            {
                var created = await controls.CreateEngagementAsync(Tenant(context), UserId(context), command, ct);
                return Results.Created($"/api/finance/v2/audit/engagements/{created.Engagement.Id}", new
                {
                    created.Engagement.Id, created.Engagement.AuditorName, created.Engagement.AuditorEmail,
                    created.Engagement.ExpiresAt, token = created.Token
                });
            }
            catch (ArgumentException ex) { return Results.UnprocessableEntity(new { success = false, error = ex.Message }); }
        });

        group.MapPost("/audit/engagements/{id:guid}/revoke", async (Guid id, HttpContext context, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            try { await controls.RevokeEngagementAsync(Tenant(context), id, UserId(context), ct); return Results.NoContent(); }
            catch (InvalidOperationException) { return Results.NotFound(); }
        });

        group.MapGet("/audit/sod-rules", async (HttpContext context, AegisDbContext db, CancellationToken ct) =>
        {
            if (!CanAuditFinance(context)) return Results.Forbid();
            return Results.Ok(await db.FinanceSodRules.AsNoTracking().Where(x => x.TenantId == Tenant(context)).OrderBy(x => x.Code).ToListAsync(ct));
        });

        group.MapPut("/audit/sod-rules", async (HttpContext context, ConfigureSodRuleCommand command, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            if (!CanManageFinance(context)) return Results.Forbid();
            try { return Results.Ok(await controls.ConfigureSodRuleAsync(Tenant(context), command, ct)); }
            catch (ArgumentException ex) { return Results.UnprocessableEntity(new { success = false, error = ex.Message }); }
        });

        group.MapPost("/audit/soc2-evidence", async (HttpContext context, GenerateSoc2EvidenceCommand command, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            if (!CanAuditFinance(context)) return Results.Forbid();
            try { return Results.Ok(await controls.GenerateSoc2EvidenceAsync(Tenant(context), UserId(context), command, ct)); }
            catch (ArgumentException ex) { return Results.UnprocessableEntity(new { success = false, error = ex.Message }); }
        });

        var auditor = endpoints.MapGroup("/api/auditor").WithTags("External Auditor");
        auditor.AddEndpointFilter(async (invocationContext, next) =>
        {
            invocationContext.HttpContext.Response.Headers.CacheControl = "no-store";
            invocationContext.HttpContext.Response.Headers.Pragma = "no-cache";
            return await next(invocationContext);
        });
        auditor.MapGet("/session", async (HttpContext context, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            var engagement = await controls.AuthenticateAsync(AuditorToken(context), "controls:read", ct);
            return engagement is null ? Results.Unauthorized() : Results.Ok(new { engagement.Id, engagement.AuditorName, engagement.AuditorEmail, engagement.ExpiresAt, scopes = JsonSerializer.Deserialize<string[]>(engagement.ScopesJson) });
        });
        auditor.MapGet("/controls", async (HttpContext context, AegisDbContext db, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            var engagement = await controls.AuthenticateAsync(AuditorToken(context), "controls:read", ct);
            if (engagement is null) return Results.Unauthorized();
            var runs = await db.FinanceControlEvaluationBatches.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == engagement.TenantId).OrderByDescending(x => x.EvaluatedAt).Take(100).ToListAsync(ct);
            return Results.Ok(new { library = controls.GetLibrary(), runs });
        });
        auditor.MapGet("/ledger", async (DateTime? from, DateTime? to, HttpContext context, AegisDbContext db, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            var engagement = await controls.AuthenticateAsync(AuditorToken(context), "ledger:read", ct);
            if (engagement is null) return Results.Unauthorized();
            var start = (from ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
            var end = (to ?? DateTime.UtcNow).ToUniversalTime();
            if (start >= end || end - start > TimeSpan.FromDays(366)) return Results.UnprocessableEntity(new { error = "invalid_period" });
            var entries = await db.JournalEntries.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == engagement.TenantId && x.IsPosted && !x.IsSandbox && x.EntryDate >= start && x.EntryDate <= end)
                .OrderBy(x => x.EntryDate).Take(5000).Select(x => new { x.Id, x.EntryNumber, x.EntryDate, x.Description, x.TotalDebit, x.TotalCredit, x.EntryHash, x.PreviousHash, x.PostedAt }).ToListAsync(ct);
            return Results.Ok(new { period = new { start, end }, entries });
        });
        auditor.MapPost("/samples", async (HttpContext context, CreateAuditSampleCommand command, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            var engagement = await controls.AuthenticateAsync(AuditorToken(context), "samples:write", ct);
            if (engagement is null) return Results.Unauthorized();
            try { return Results.Created("/api/auditor/samples", await controls.CreateSampleAsync(engagement, command, ct)); }
            catch (ArgumentException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
        });
        auditor.MapGet("/samples", async (HttpContext context, AegisDbContext db, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            var engagement = await controls.AuthenticateAsync(AuditorToken(context), "controls:read", ct);
            if (engagement is null) return Results.Unauthorized();
            return Results.Ok(await db.FinanceAuditSamples.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == engagement.TenantId && x.AuditorEngagementId == engagement.Id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct));
        });
        auditor.MapGet("/evidence", async (HttpContext context, AegisDbContext db, FinanceContinuousControlService controls, CancellationToken ct) =>
        {
            var engagement = await controls.AuthenticateAsync(AuditorToken(context), "evidence:read", ct);
            if (engagement is null) return Results.Unauthorized();
            var packages = await db.FinanceAuditEvidencePackages.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == engagement.TenantId && (x.AuditorEngagementId == null || x.AuditorEngagementId == engagement.Id)).OrderByDescending(x => x.GeneratedAt)
                .Select(x => new { x.Id, x.Framework, x.PeriodFrom, x.PeriodTo, x.ContentHash, x.PqcAlgorithm, x.PqcKeyId, x.PqcSignature, x.PqcPublicKey, x.GeneratedAt }).ToListAsync(ct);
            return Results.Ok(packages.Select(x => new { x.Id, x.Framework, x.PeriodFrom, x.PeriodTo, x.ContentHash, x.PqcAlgorithm, x.PqcKeyId, signature = Convert.ToBase64String(x.PqcSignature), publicKey = Convert.ToBase64String(x.PqcPublicKey), x.GeneratedAt, download = $"/api/auditor/evidence/{x.Id:D}/download" }));
        });
        auditor.MapGet("/evidence/{id:guid}/download", async (Guid id, HttpContext context, AegisDbContext db, FinanceContinuousControlService controls, IR2StorageService storage, CancellationToken ct) =>
        {
            var engagement = await controls.AuthenticateAsync(AuditorToken(context), "evidence:read", ct);
            if (engagement is null) return Results.Unauthorized();
            var package = await db.FinanceAuditEvidencePackages.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefaultAsync(x => x.TenantId == engagement.TenantId && x.Id == id && (x.AuditorEngagementId == null || x.AuditorEngagementId == engagement.Id), ct);
            if (package is null) return Results.NotFound();
            var stream = await storage.DownloadAsync(package.StorageKey, ct);
            return stream is null ? Results.NotFound() : Results.Stream(stream, "application/json", $"aegiscore-soc2-{package.PeriodFrom:yyyyMMdd}-{package.PeriodTo:yyyyMMdd}.json");
        });

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

    private static Guid? StudentId(HttpContext context)
        => Guid.TryParse(context.User.FindFirstValue("student_id"), out var student) && student != Guid.Empty ? student : null;

    private static Guid? DepartmentId(HttpContext context)
        => Guid.TryParse(context.User.FindFirstValue("department_id"), out var department) && department != Guid.Empty ? department : null;

    private static bool CanManageFinance(HttpContext context)
        => context.User.IsInRole("Finance") || context.User.IsInRole("Finance Controller") || context.User.IsInRole("CFO") || context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin");

    private static bool CanAuditFinance(HttpContext context) => CanManageFinance(context) || context.User.IsInRole("Auditor");

    // Read-only view of the executive dashboard. The VC reads collection figures but does not
    // post, approve or sign anything, so this deliberately does not widen CanManageFinance.
    private static bool CanViewExecutiveFinance(HttpContext context) => CanManageFinance(context) || context.User.IsInRole("VC");

    private static string? AuditorToken(HttpContext context) => context.Request.Headers["X-AegisCore-Auditor-Token"].FirstOrDefault();

    private static string StudentPaymentAlias(string? profileJson)
    {
        if (string.IsNullOrWhiteSpace(profileJson)) return string.Empty;
        try
        {
            using var document = JsonDocument.Parse(profileJson);
            foreach (var name in new[] { "paymentAlias", "registeredMobile", "mobile", "phone" })
                if (document.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString()?.Trim() ?? string.Empty;
        }
        catch (JsonException) { }
        return string.Empty;
    }

    private static bool ValidCurrency(string value) => value.Length == 3 && value.All(c => c is >= 'A' and <= 'Z');

    private static async Task<object> BuildReportAsync(string reportType, Guid tenantId, DateTime? from, DateTime? to, DateTime? asOf, IFinanceModuleService finance, CancellationToken ct)
        => reportType switch
        {
            "trial-balance" => new { from = from ?? DateTime.UtcNow.Date, to = to ?? DateTime.UtcNow, lines = await finance.GetTrialBalanceAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct) },
            "cash-position" => await finance.GetCashPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
            "statement-of-financial-position" => await finance.GetFinancialPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
            "profit-loss" => await finance.GetProfitLossAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct),
            "receivables-aging" => await finance.GetReceivablesAgingAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(reportType))
        };

    private static byte[] ReportCsv(object report)
    {
        using var document = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(report, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = document.RootElement;
        var rows = new List<(string Category, JsonElement Value)>();
        if (root.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
            foreach (var line in lines.EnumerateArray()) rows.Add(("", line));
        foreach (var category in new[] { "assets", "liabilities", "equity", "revenue", "expenses" })
            if (root.TryGetProperty(category, out var values) && values.ValueKind == JsonValueKind.Array)
                foreach (var value in values.EnumerateArray()) rows.Add((category, value));
        if (rows.Count == 0) rows.Add(("", root));
        var columns = rows.SelectMany(x => x.Value.ValueKind == JsonValueKind.Object ? x.Value.EnumerateObject().Select(p => p.Name) : []).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', new[] { "category" }.Concat(columns).Select(CsvCell)));
        foreach (var row in rows)
        {
            builder.Append(CsvCell(row.Category));
            foreach (var column in columns)
            {
                builder.Append(',');
                if (row.Value.ValueKind == JsonValueKind.Object && row.Value.TryGetProperty(column, out var value)) builder.Append(CsvCell(value));
            }
            builder.AppendLine();
        }
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string CsvCell(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    private static string CsvCell(JsonElement value) => CsvCell(value.ValueKind == JsonValueKind.Null ? string.Empty : value.ToString());

    private static byte[] ReportXlsx(object report)
    {
        using var document = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(report, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = document.RootElement;
        var rows = new List<(string Category, Dictionary<string, string> Values)>();
        if (root.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
            foreach (var line in lines.EnumerateArray()) rows.Add((string.Empty, ObjectValues(line)));
        foreach (var category in new[] { "assets", "liabilities", "equity", "revenue", "expenses" })
            if (root.TryGetProperty(category, out var values) && values.ValueKind == JsonValueKind.Array)
                foreach (var value in values.EnumerateArray()) rows.Add((category, ObjectValues(value)));
        if (rows.Count == 0) rows.Add((string.Empty, ObjectValues(root)));
        var columns = rows.SelectMany(x => x.Values.Keys).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddZipEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            AddZipEntry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            AddZipEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Report\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            AddZipEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            AddXlsxRow(sheet, new[] { "category" }.Concat(columns));
            foreach (var row in rows) AddXlsxRow(sheet, new[] { row.Category }.Concat(columns.Select(column => row.Values.GetValueOrDefault(column, string.Empty))));
            sheet.Append("</sheetData></worksheet>");
            AddZipEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return stream.ToArray();
    }

    private static Dictionary<string, string> ObjectValues(JsonElement value)
        => value.ValueKind == JsonValueKind.Object
            ? value.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ValueKind == JsonValueKind.Null ? string.Empty : x.Value.ToString(), StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal) { ["value"] = value.ToString() };

    private static void AddXlsxRow(StringBuilder sheet, IEnumerable<string> values)
    {
        sheet.Append("<row>");
        foreach (var value in values) sheet.Append("<c t=\"inlineStr\"><is><t>").Append(System.Security.SecurityElement.Escape(value) ?? string.Empty).Append("</t></is></c>");
        sheet.Append("</row>");
    }

    private static void AddZipEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static byte[] ReportPdf(object report, string verificationUrl)
    {
        var lines = Encoding.UTF8.GetString(ReportCsv(report)).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var qr = new PngByteQRCode(new QRCodeGenerator().CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q)).GetGraphic(4);
        using var document = new PdfDocument();
        var font = new XFont("Helvetica", 8, XFontStyle.Regular);
        var titleFont = new XFont("Helvetica", 14, XFontStyle.Bold);
        const int linesPerPage = 34;
        for (var offset = 0; offset < Math.Max(1, lines.Length); offset += linesPerPage)
        {
            var page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.DrawString("AegisCore Finance Report", titleFont, XBrushes.Black, new XRect(36, 28, 500, 24), XStringFormats.TopLeft);
            graphics.DrawString($"Verification: {verificationUrl}", font, XBrushes.DarkSlateGray, new XRect(36, 54, 500, 16), XStringFormats.TopLeft);
            using var qrImage = XImage.FromStream(() => new MemoryStream(qr, writable: false));
            graphics.DrawImage(qrImage, page.Width - 92, 24, 56, 56);
            var row = 0;
            foreach (var line in lines.Skip(offset).Take(linesPerPage))
            {
                var display = line.Length > 150 ? line[..150] : line;
                graphics.DrawString(display, font, XBrushes.Black, new XRect(36, 88 + row * 17, page.Width - 72, 16), XStringFormats.TopLeft);
                row++;
            }
            graphics.DrawString($"Page {offset / linesPerPage + 1}", font, XBrushes.Gray, new XRect(36, page.Height - 34, 500, 16), XStringFormats.TopLeft);
        }
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static async Task<IResult> CreateSignedXmlReport(string reportType, string format, string extension, string contentType, bool inline, DateTime? from, DateTime? to, DateTime? asOf, HttpContext context, IFinanceModuleService finance, AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, CancellationToken ct)
    {
        if (!CanManageFinance(context)) return Results.Forbid();
        var tenantId = Tenant(context);
        var normalized = reportType.Trim().ToLowerInvariant();
        if (normalized is not ("trial-balance" or "cash-position" or "statement-of-financial-position" or "profit-loss" or "receivables-aging"))
            return Results.UnprocessableEntity(new { success = false, error = "Unsupported finance report type." });
        object report = normalized switch
        {
            "trial-balance" => new { from = from ?? DateTime.UtcNow.Date, to = to ?? DateTime.UtcNow, lines = await finance.GetTrialBalanceAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct) },
            "cash-position" => await finance.GetCashPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
            "statement-of-financial-position" => await finance.GetFinancialPositionAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
            "profit-loss" => await finance.GetProfitLossAsync(tenantId, from ?? DateTime.UtcNow.Date, to ?? DateTime.UtcNow, ct),
            "receivables-aging" => await finance.GetReceivablesAgingAsync(tenantId, asOf ?? DateTime.UtcNow, ct),
            _ => throw new InvalidOperationException("Report type validation failed.")
        };
        var artifactId = Guid.NewGuid();
        var verificationUrl = $"/api/finance/v2/reports/signed/{artifactId:D}/verify";
        var payload = ReportXbrl(report, verificationUrl, inline);
        var signature = await pqc.SignAsync(tenantId, "finance-report", payload, archival: false, ct);
        var storageKey = $"finance-reports/{tenantId:D}/{DateTime.UtcNow:yyyy/MM}/{artifactId:N}{extension}";
        await storage.UploadAsync(storageKey, new MemoryStream(payload), contentType, ct);
        var artifact = new FinanceReportArtifact
        {
            Id = artifactId, TenantId = tenantId, ReportType = normalized, Format = format, PeriodLabel = $"{from:O}|{to:O}|{asOf:O}", StorageKey = storageKey,
            ContentHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(), PqcAlgorithm = signature.Algorithm, PqcKeyId = signature.KeyId, PqcSignature = signature.Signature, PqcPublicKey = signature.PublicKey, GeneratedByUserId = UserId(context)
        };
        db.FinanceReportArtifacts.Add(artifact);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { artifact.Id, artifact.ReportType, artifact.Format, artifact.StorageKey, artifact.ContentHash, artifact.PqcAlgorithm, artifact.PqcKeyId, signature = Convert.ToBase64String(artifact.PqcSignature), publicKey = Convert.ToBase64String(artifact.PqcPublicKey), verification = verificationUrl });
    }

    private static byte[] ReportXbrl(object report, string verificationUrl, bool inline)
    {
        using var document = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(report, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var facts = new List<(string Name, string Value, bool Numeric)>();
        CollectFacts(document.RootElement, facts, "fact");
        var escapedUrl = System.Security.SecurityElement.Escape(verificationUrl) ?? string.Empty;
        var builder = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        if (inline)
        {
            builder.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:ix=\"http://www.xbrl.org/2013/inlineXBRL\" xmlns:aegis=\"https://aegiscore.example/taxonomy\"><head><title>AegisCore Finance Report</title></head><body><ix:header><ix:references><ix:linkbaseRef xlink:type=\"simple\" xlink:href=\"aegis-taxonomy.xsd\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"/></ix:references></ix:header><p>Verification: ").Append(escapedUrl).Append("</p>");
            foreach (var fact in facts)
            {
                var name = XmlFactName(fact.Name);
                var value = System.Security.SecurityElement.Escape(fact.Value) ?? string.Empty;
                builder.Append(fact.Numeric ? "<ix:nonFraction name=\"aegis:" : "<ix:nonNumeric name=\"aegis:").Append(name).Append("\" contextRef=\"c1\">").Append(value).Append(fact.Numeric ? "</ix:nonFraction>" : "</ix:nonNumeric>");
            }
            builder.Append("</body></html>");
        }
        else
        {
            builder.Append("<xbrl xmlns=\"http://www.xbrl.org/2003/instance\" xmlns:aegis=\"https://aegiscore.example/taxonomy\"><link:schemaRef xlink:type=\"simple\" xlink:href=\"aegis-taxonomy.xsd\" xmlns:link=\"http://www.xbrl.org/2003/linkbase\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"/><context id=\"c1\"><entity><identifier scheme=\"https://aegiscore.example\">AegisCore</identifier></entity><period><instant>" ).Append(DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)).Append("</instant></period></context>");
            foreach (var fact in facts)
            {
                builder.Append("<aegis:").Append(XmlFactName(fact.Name)).Append(" contextRef=\"c1\"");
                if (fact.Numeric) builder.Append(" decimals=\"2\"");
                builder.Append(">").Append(System.Security.SecurityElement.Escape(fact.Value) ?? string.Empty).Append("</aegis:").Append(XmlFactName(fact.Name)).Append(">");
            }
            builder.Append("</xbrl>");
        }
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static void CollectFacts(JsonElement value, ICollection<(string Name, string Value, bool Numeric)> facts, string prefix)
    {
        if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject()) CollectFacts(property.Value, facts, $"{prefix}_{property.Name}");
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) CollectFacts(item, facts, prefix);
        else if (value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
            facts.Add((prefix, value.ToString(), value.ValueKind is JsonValueKind.Number));
    }

    private static string XmlFactName(string value)
    {
        var name = new string(value.Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_').ToArray());
        return string.IsNullOrWhiteSpace(name) || char.IsAsciiDigit(name[0]) ? $"f_{name}" : name;
    }

    private static async Task<bool> HasFcyExposureAsync(AegisDbContext db, Guid tenantId, string functionalCurrency, CancellationToken ct)
        => await db.FeeDemands.AnyAsync(x => x.TenantId == tenantId && x.Currency != functionalCurrency, ct)
            || await db.FinanceVendors.AnyAsync(x => x.TenantId == tenantId && x.Status == "ACTIVE" && x.InvoiceCurrency != functionalCurrency, ct);

    private static bool VerifySeal(Guid tenantId, FinanceAuditSeal seal, IPqcSignatureService pqc)
    {
        var payload = System.Text.Encoding.UTF8.GetBytes($"{tenantId:D}|{seal.PeriodStart:O}|{seal.PeriodEnd:O}|{seal.EventCount}|{Convert.ToHexString(seal.MerkleRoot)}");
        return pqc.Verify(seal.Algorithm, payload, seal.Signature, seal.PublicKey);
    }

}
