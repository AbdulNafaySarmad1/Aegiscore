using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Application.Finance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed class FinanceContinuousControlService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyList<FinanceControlDefinition> Library = LoadLibrary();
    private static readonly string[] AuditorScopes = ["controls:read", "ledger:read", "samples:write", "evidence:read"];

    private readonly AegisDbContext _db;
    private readonly IPqcSignatureService _pqc;
    private readonly IR2StorageService _storage;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FinanceContinuousControlService(AegisDbContext db, IPqcSignatureService pqc, IR2StorageService storage, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _pqc = pqc;
        _storage = storage;
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyList<FinanceControlDefinition> GetLibrary() => Library;

    public async Task<FinanceControlEvaluationBatch> EvaluatePostingAsync(Guid tenantId, Guid voucherId, CancellationToken ct)
    {
        var voucher = await _db.FinanceVouchers.AsNoTracking().Include(x => x.Lines).Include(x => x.Approvals)
            .SingleAsync(x => x.TenantId == tenantId && x.Id == voucherId, ct);
        var metrics = await BuildMetricsAsync(tenantId, voucher, ct);
        var results = Library.Select(definition => Evaluate(definition, metrics)).ToArray();
        var json = JsonSerializer.Serialize(results, JsonOptions);
        var batch = new FinanceControlEvaluationBatch
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Trigger = "VOUCHER_POSTED", ReferenceType = "FinanceVoucher", ReferenceId = voucherId,
            ControlCount = results.Length, PassedCount = results.Count(x => x.Outcome == "PASS"), FlaggedCount = results.Count(x => x.Outcome == "FLAG"),
            ResultsJson = json, ContentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant(), EvaluatedAt = DateTime.UtcNow
        };
        _db.FinanceControlEvaluationBatches.Add(batch);
        await _db.SaveChangesAsync(ct);
        return batch;
    }

    public async Task<(FinanceAuditorEngagement Engagement, string Token)> CreateEngagementAsync(Guid tenantId, Guid actorId, CreateAuditorEngagementCommand command, CancellationToken ct)
    {
        var name = command.AuditorName.Trim();
        var email = command.AuditorEmail.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        if (name.Length is < 2 or > 200 || !System.Net.Mail.MailAddress.TryCreate(email, out _) || command.ExpiresAt <= now || command.ExpiresAt > now.AddDays(90))
            throw new ArgumentException("Auditor name, email, and an expiry within 90 days are required.");
        var scopes = command.Scopes.Distinct(StringComparer.Ordinal).ToArray();
        if (scopes.Length == 0 || scopes.Except(AuditorScopes, StringComparer.Ordinal).Any()) throw new ArgumentException("Auditor scopes are invalid.");
        var token = CreateToken(tenantId);
        var engagement = new FinanceAuditorEngagement
        {
            Id = Guid.NewGuid(), TenantId = tenantId, AuditorName = name, AuditorEmail = email, TokenHash = HashToken(token),
            ScopesJson = JsonSerializer.Serialize(scopes, JsonOptions), CreatedByUserId = actorId, CreatedAt = now, ExpiresAt = command.ExpiresAt.ToUniversalTime()
        };
        _db.FinanceAuditorEngagements.Add(engagement);
        await _db.SaveChangesAsync(ct);
        return (engagement, token);
    }

    public async Task RevokeEngagementAsync(Guid tenantId, Guid engagementId, Guid actorId, CancellationToken ct)
    {
        var engagement = await _db.FinanceAuditorEngagements.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == engagementId, ct)
            ?? throw new InvalidOperationException("Auditor engagement was not found.");
        if (engagement.RevokedAt is null) { engagement.RevokedAt = DateTime.UtcNow; engagement.RevokedByUserId = actorId; await _db.SaveChangesAsync(ct); }
    }

    public async Task<FinanceAuditorEngagement?> AuthenticateAsync(string? token, string requiredScope, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200 || !AuditorScopes.Contains(requiredScope, StringComparer.Ordinal)) return null;
        if (!TryReadTokenTenant(token, out var tenantId)) return null;
        if (_httpContextAccessor.HttpContext is { } httpContext) httpContext.Items["TenantId"] = tenantId;
        await _db.Database.OpenConnectionAsync(ct);
        var hash = HashToken(token);
        var candidates = await _db.FinanceAuditorEngagements.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TokenHash == hash && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow).ToListAsync(ct);
        var engagement = candidates.FirstOrDefault(x => CryptographicOperations.FixedTimeEquals(Convert.FromHexString(x.TokenHash), Convert.FromHexString(hash)));
        return engagement is not null && JsonSerializer.Deserialize<string[]>(engagement.ScopesJson, JsonOptions)!.Contains(requiredScope, StringComparer.Ordinal) ? engagement : null;
    }

    public async Task<FinanceAuditSample> CreateSampleAsync(FinanceAuditorEngagement engagement, CreateAuditSampleCommand command, CancellationToken ct)
    {
        var population = command.PopulationType.Trim().ToUpperInvariant();
        var from = command.PeriodFrom.ToUniversalTime();
        var to = command.PeriodTo.ToUniversalTime();
        if (population != "VOUCHERS" || from >= to || to - from > TimeSpan.FromDays(3660) || command.SampleSize is < 1 or > 500 || command.Seed.Length is < 16 or > 200)
            throw new ArgumentException("Sampling parameters are invalid.");
        var references = await _db.FinanceVouchers.AsNoTracking()
            .Where(x => x.TenantId == engagement.TenantId && x.Status == FinanceStates.Posted && x.CreatedAt >= from && x.CreatedAt <= to)
            .Select(x => new { x.Id, x.VoucherNumber }).ToListAsync(ct);
        var selected = references.OrderBy(x => DeterministicRank(command.Seed, x.Id)).Take(Math.Min(command.SampleSize, references.Count)).ToArray();
        var sample = new FinanceAuditSample
        {
            Id = Guid.NewGuid(), TenantId = engagement.TenantId, AuditorEngagementId = engagement.Id, PopulationType = population,
            PeriodFrom = from, PeriodTo = to, PopulationCount = references.Count, SampleSize = selected.Length,
            SeedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.Seed))).ToLowerInvariant(),
            SelectedReferencesJson = JsonSerializer.Serialize(selected, JsonOptions), CreatedAt = DateTime.UtcNow
        };
        _db.FinanceAuditSamples.Add(sample);
        await _db.SaveChangesAsync(ct);
        return sample;
    }

    public async Task<FinanceAuditEvidencePackage> GenerateSoc2EvidenceAsync(Guid tenantId, Guid actorId, GenerateSoc2EvidenceCommand command, CancellationToken ct)
    {
        var from = command.PeriodFrom.ToUniversalTime();
        var to = command.PeriodTo.ToUniversalTime();
        if (from >= to || to - from > TimeSpan.FromDays(366)) throw new ArgumentException("Evidence period must be valid and no longer than 366 days.");
        if (command.AuditorEngagementId.HasValue && !await _db.FinanceAuditorEngagements.AnyAsync(x => x.TenantId == tenantId && x.Id == command.AuditorEngagementId && x.RevokedAt == null, ct))
            throw new ArgumentException("Auditor engagement is invalid.");
        var evaluations = await _db.FinanceControlEvaluationBatches.AsNoTracking().Where(x => x.TenantId == tenantId && x.EvaluatedAt >= from && x.EvaluatedAt <= to).OrderBy(x => x.EvaluatedAt).ToListAsync(ct);
        var auditEvents = await _db.AuditLogs.AsNoTracking().Where(x => x.TenantId == tenantId && x.Timestamp >= from && x.Timestamp <= to).OrderBy(x => x.Timestamp).Select(x => new { x.Id, x.UserId, x.Action, x.ResourceType, x.ResourceId, x.Success, x.Timestamp, x.IpAddress }).ToListAsync(ct);
        var seals = await _db.FinanceAuditSeals.AsNoTracking().Where(x => x.TenantId == tenantId && x.SealedAt >= from && x.SealedAt <= to).OrderBy(x => x.SealedAt).Select(x => new { x.Id, x.PeriodStart, x.PeriodEnd, x.EventCount, x.Algorithm, x.KeyId, merkleRoot = Convert.ToHexString(x.MerkleRoot).ToLowerInvariant(), x.SealedAt }).ToListAsync(ct);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new { schema = "AegisCore.SOC2.Evidence.v1", tenantId, period = new { from, to }, controlLibrary = Library, evaluations, auditEvents, seals, generatedAt = DateTime.UtcNow }, JsonOptions);
        var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        var signature = await _pqc.SignAsync(tenantId, "audit-evidence", payload, archival: false, ct);
        var id = Guid.NewGuid();
        var key = $"audit/{tenantId:D}/{from:yyyyMMdd}-{to:yyyyMMdd}/{id:D}.json";
        await using var stream = new MemoryStream(payload, writable: false);
        await _storage.UploadAsync(key, stream, "application/json", ct);
        var package = new FinanceAuditEvidencePackage
        {
            Id = id, TenantId = tenantId, AuditorEngagementId = command.AuditorEngagementId, Framework = "SOC2_TYPE_II",
            PeriodFrom = from, PeriodTo = to, StorageKey = key, ContentHash = hash, PqcAlgorithm = signature.Algorithm,
            PqcKeyId = signature.KeyId, PqcSignature = signature.Signature, PqcPublicKey = signature.PublicKey,
            GeneratedByUserId = actorId, GeneratedAt = DateTime.UtcNow
        };
        _db.FinanceAuditEvidencePackages.Add(package);
        await _db.SaveChangesAsync(ct);
        return package;
    }

    public async Task<FinanceSodRule> ConfigureSodRuleAsync(Guid tenantId, ConfigureSodRuleCommand command, CancellationToken ct)
    {
        var code = command.Code.Trim().ToUpperInvariant();
        var actions = command.IncompatibleActions.Select(x => x.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        if (code.Length is < 3 or > 50 || command.Description.Trim().Length is < 5 or > 500 || actions.Length < 2 || actions.Any(x => x.Length is < 3 or > 50)) throw new ArgumentException("SoD rule is invalid.");
        var rule = await _db.FinanceSodRules.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);
        if (rule is null) { rule = new FinanceSodRule { Id = Guid.NewGuid(), TenantId = tenantId, Code = code, CreatedAt = DateTime.UtcNow }; _db.FinanceSodRules.Add(rule); }
        rule.Description = command.Description.Trim(); rule.IncompatibleActionsJson = JsonSerializer.Serialize(actions, JsonOptions); rule.IsActive = command.IsActive; rule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return rule;
    }

    private async Task<Dictionary<string, decimal>> BuildMetricsAsync(Guid tenantId, FinanceVoucher voucher, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var approved = voucher.Approvals.Where(x => x.Decision == "APPROVED").ToArray();
        var accountIds = voucher.Lines.Select(x => x.ChartOfAccountId).Distinct().ToArray();
        var activeAccounts = await _db.ChartOfAccounts.CountAsync(x => x.TenantId == tenantId && x.IsActive && accountIds.Contains(x.Id), ct);
        var attachments = await _db.FinanceVoucherAttachments.CountAsync(x => x.TenantId == tenantId && x.FinanceVoucherId == voucher.Id, ct);
        var rule = await _db.FinanceVoucherApprovalRules.AsNoTracking().Where(x => x.TenantId == tenantId && x.Currency == voucher.Currency && x.IsActive && voucher.TotalAmount >= x.MinimumAmount && (x.MaximumAmount == null || voucher.TotalAmount < x.MaximumAmount)).OrderByDescending(x => x.MinimumAmount).FirstOrDefaultAsync(ct);
        var lastSeal = await _db.FinanceAuditSeals.Where(x => x.TenantId == tenantId).MaxAsync(x => (DateTime?)x.SealedAt, ct);
        var totalPortfolio = await _db.FinanceInvestmentHoldings.Where(x => x.TenantId == tenantId && x.Status == "HELD").SumAsync(x => (decimal?)x.MarketValue, ct) ?? 0m;
        var issuerMax = await _db.FinanceInvestmentHoldings.Where(x => x.TenantId == tenantId && x.Status == "HELD").GroupBy(x => x.Issuer).Select(x => x.Sum(y => y.MarketValue)).OrderByDescending(x => x).FirstOrDefaultAsync(ct);
        var metrics = new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["voucher_balanced"] = Bool(voucher.Lines.Sum(x => x.Debit) == voucher.Lines.Sum(x => x.Credit)),
            ["maker_checker"] = Bool(voucher.ApprovedByUserId.HasValue && voucher.ApprovedByUserId != voucher.CreatedByUserId),
            ["approval_signature"] = Bool(approved.Length > 0 && approved.All(x => x.PqcSignature is { Length: > 0 })),
            ["voucher_signature"] = Bool(voucher.PqcSignature is { Length: > 0 }),
            ["approval_before_post"] = Bool(voucher.ApprovedAt.HasValue && voucher.PostedAt.HasValue && voucher.ApprovedAt <= voucher.PostedAt),
            ["poster_identified"] = Bool(voucher.PostedByUserId.HasValue && voucher.PostedByUserId != Guid.Empty),
            ["valid_currency"] = Bool(voucher.Currency.Length == 3 && voucher.Currency.All(char.IsAsciiLetter)),
            ["nonzero_lines"] = Bool(voucher.Lines.Count >= 2),
            ["single_sided_lines"] = Bool(voucher.Lines.All(x => x.Debit >= 0m && x.Credit >= 0m && (x.Debit == 0m) != (x.Credit == 0m))),
            ["active_accounts"] = Bool(activeAccounts == accountIds.Length),
            ["attachment_compliance"] = Bool(voucher.TotalAmount < 50000m || attachments > 0),
            ["positive_amount"] = Bool(voucher.TotalAmount > 0m),
            ["not_future_dated"] = Bool(voucher.CreatedAt <= now.AddMinutes(5)),
            ["backdate_days"] = (decimal)Math.Max(0d, ((voucher.PostedAt ?? now) - voucher.CreatedAt).TotalDays),
            ["description_present"] = Bool(!string.IsNullOrWhiteSpace(voucher.Description)),
            ["description_length"] = voucher.Description?.Trim().Length ?? 0,
            ["approval_quorum"] = Bool(rule is null ? approved.Length > 0 : approved.Select(x => x.ApproverUserId).Distinct().Count() >= rule.MinimumApprovals),
            ["exact_duplicate_count"] = await _db.FinanceVouchers.CountAsync(x => x.TenantId == tenantId && x.Id != voucher.Id && x.TotalAmount == voucher.TotalAmount && x.Currency == voucher.Currency && x.Description == voucher.Description && x.Status == FinanceStates.Posted, ct) + 1,
            ["normal_hours"] = Bool(voucher.PostedAt is { Hour: >= 7 and <= 21 }),
            ["round_number_risk"] = Bool(voucher.TotalAmount >= 10000m && decimal.Remainder(voucher.TotalAmount, 1000m) == 0m),
            ["unbalanced_journal_count"] = await _db.JournalEntries.CountAsync(x => x.TenantId == tenantId && x.IsPosted && x.TotalDebit != x.TotalCredit, ct),
            ["missing_chain_hash_count"] = await _db.JournalEntries.CountAsync(x => x.TenantId == tenantId && x.IsPosted && (x.EntryHash == null || x.EntryHash.Length == 0), ct),
            ["missing_ledger_signature_count"] = await _db.JournalEntries.CountAsync(x => x.TenantId == tenantId && x.IsPosted && !_db.FinanceLedgerSignatures.Any(s => s.TenantId == tenantId && s.JournalEntryId == x.Id), ct),
            ["seal_age_minutes"] = lastSeal.HasValue ? (decimal)Math.Max(0d, (now - lastSeal.Value).TotalMinutes) : decimal.MaxValue,
            ["expired_active_key_count"] = await _db.FinancePqcKeys.CountAsync(x => x.TenantId == tenantId && x.IsActive && x.ExpiresAt <= now, ct),
            ["dead_key_count"] = await _db.FinancePqcKeys.CountAsync(x => x.TenantId == tenantId && x.IsActive && (x.LastUsedAt == null ? x.CreatedAt < now.AddDays(-30) : x.LastUsedAt < now.AddDays(-30)), ct),
            ["gateway_error_count"] = await _db.PaymentGatewayConfigurations.CountAsync(x => x.TenantId == tenantId && x.IsActive && x.HealthStatus == "error", ct),
            ["duplicate_webhook_count"] = await _db.FinancePayments.Where(x => x.TenantId == tenantId && x.WebhookId != null).GroupBy(x => x.WebhookId).CountAsync(x => x.Count() > 1, ct),
            ["missing_payment_reference_count"] = await _db.FinancePayments.CountAsync(x => x.TenantId == tenantId && x.Status == "SETTLED" && x.Method == "WEBHOOK" && (x.ExternalReference == null || x.ExternalReference == ""), ct),
            ["sandbox_live_posting_count"] = await SandboxLivePostingCountAsync(tenantId, ct),
            ["unscreened_vendor_count"] = await _db.FinanceVendors.CountAsync(x => x.TenantId == tenantId && x.Status == "ACTIVE" && (x.LastSanctionsScreenedAt == null || x.LastSanctionsScreenedAt < now.AddDays(-90)), ct),
            ["sanctioned_active_vendor_count"] = await _db.FinanceVendors.CountAsync(x => x.TenantId == tenantId && x.Status == "ACTIVE" && x.SanctionsStatus != "CLEAR" && x.SanctionsStatus != "PENDING", ct),
            ["payable_vendor_bank_gap_count"] = await (from invoice in _db.FinanceVendorInvoices join vendor in _db.FinanceVendors on invoice.VendorId equals vendor.Id where invoice.TenantId == tenantId && vendor.TenantId == tenantId && invoice.PaymentVoucherId != null && vendor.Iban == null && vendor.RaastId == null select invoice.Id).CountAsync(ct),
            ["unmatched_strategic_invoice_count"] = await (from invoice in _db.FinanceVendorInvoices join vendor in _db.FinanceVendors on invoice.VendorId equals vendor.Id where invoice.TenantId == tenantId && vendor.TenantId == tenantId && vendor.Tier == "TIER1" && invoice.Status == "RECEIVED" select invoice.Id).CountAsync(ct),
            ["receipt_before_approval_count"] = await (from receipt in _db.FinanceGoodsReceipts join order in _db.FinancePurchaseOrders on receipt.PurchaseOrderId equals order.Id where receipt.TenantId == tenantId && order.TenantId == tenantId && (order.ApprovedAt == null || receipt.ReceivedAt < order.ApprovedAt) select receipt.Id).CountAsync(ct),
            ["invoice_tolerance_exception_count"] = await _db.FinanceThreeWayMatches.CountAsync(x => x.TenantId == tenantId && x.Status == "FAILED", ct),
            ["expired_active_contract_count"] = await _db.FinanceVendorContracts.CountAsync(x => x.TenantId == tenantId && x.Status == "ACTIVE" && x.EndsOn < DateOnly.FromDateTime(now), ct),
            ["blanket_po_overrun_count"] = await _db.FinanceStandingPurchaseOrders.CountAsync(x => x.TenantId == tenantId && x.Status == "ACTIVE" && x.UsedAmount > x.LimitAmount, ct),
            ["stale_unmatched_bank_count"] = await _db.FinanceBankFeedTransactions.CountAsync(x => x.TenantId == tenantId && x.Status == "UNMATCHED" && x.BookingDate < now.AddDays(-7), ct),
            ["invalid_bank_currency_count"] = await _db.FinanceBankAccounts.CountAsync(x => x.TenantId == tenantId && (x.Currency.Length != 3 || x.Currency == ""), ct),
            ["fcy_rate_gap_count"] = await _db.FinanceForeignCurrencyTransactions.CountAsync(x => x.TenantId == tenantId && (x.RateUsed <= 0m || x.RateSource == ""), ct),
            ["stale_rate_count"] = await _db.FinanceForeignCurrencyTransactions.CountAsync(x => x.TenantId == tenantId && x.CreatedAt > x.RateDate.AddDays(2), ct),
            ["negative_receivable_count"] = await _db.FeeDemands.CountAsync(x => x.TenantId == tenantId && (x.OutstandingAmount < 0m || x.PaidAmount > x.TotalAmount), ct),
            ["unclassified_overdue_installment_count"] = await _db.FeeInstallments.CountAsync(x => x.TenantId == tenantId && x.DueDate < now && x.PaidAmount < x.Amount + x.PenaltyAmount && x.Status != "OVERDUE", ct),
            ["invalid_deposit_status_count"] = await _db.FinanceSecurityDeposits.CountAsync(x => x.TenantId == tenantId && (x.RefundedAmount + x.ForfeitedAmount > x.OriginalAmount || (x.Status == "HELD" && (x.RefundedAmount > 0m || x.ForfeitedAmount > 0m))), ct),
            ["overdue_deposit_refund_count"] = await _db.FinanceSecurityDeposits.CountAsync(x => x.TenantId == tenantId && x.RefundRequestedAt != null && x.ResolvedAt == null && x.RefundRequestedAt.Value.AddDays(x.RefundSlaDays) < now, ct),
            ["negative_inventory_count"] = await _db.FinanceInventoryItems.CountAsync(x => x.TenantId == tenantId && x.OnHandQuantity < 0m, ct),
            ["unposted_inventory_variance_count"] = await _db.FinanceInventoryMovements.CountAsync(x => x.TenantId == tenantId && x.MovementType == "COUNT_ADJUSTMENT" && x.JournalEntryId == null, ct),
            ["asset_policy_gap_count"] = await _db.Assets.CountAsync(x => x.TenantId == tenantId && x.Status == AssetStatus.Active && (x.UsefulLifeYears <= 0 || x.PurchaseCost > 0m && x.DepreciationMethod == 0), ct),
            ["disposed_asset_active_count"] = await (from disposal in _db.AssetDisposals join asset in _db.Assets on disposal.AssetId equals asset.Id where disposal.TenantId == tenantId && asset.TenantId == tenantId && asset.Status == AssetStatus.Active && disposal.Status == DisposalStatus.Completed select disposal.Id).CountAsync(ct),
            ["expired_insurance_count"] = await _db.FinanceInsurancePolicies.CountAsync(x => x.TenantId == tenantId && x.Status == "ACTIVE" && x.EndDate < now, ct),
            ["unapproved_overdue_utility_count"] = await _db.FinanceUtilityBills.CountAsync(x => x.TenantId == tenantId && x.DueDate < now && x.Status != "PAID" && x.Status != "RECONCILED" && x.ApprovedAt == null, ct),
            ["overspent_grant_count"] = await _db.FinanceGrants.CountAsync(x => x.TenantId == tenantId && x.SpentAmount > x.AwardAmount, ct),
            ["scholarship_budget_overrun_count"] = await _db.FinanceGrants.CountAsync(x => x.TenantId == tenantId && _db.FinanceScholarshipAwards.Where(a => a.TenantId == tenantId && a.GrantId == x.Id && a.Status != "REVOKED").Sum(a => (decimal?)a.Amount) > x.AwardAmount, ct),
            ["investment_concentration_breach_count"] = Bool(totalPortfolio > 0m && issuerMax / totalPortfolio > 0.20m)
        };
        return metrics;
    }

    private async Task<int> SandboxLivePostingCountAsync(Guid tenantId, CancellationToken ct)
    {
        var refs = await _db.PaymentGatewayEvents.Where(x => x.TenantId == tenantId && x.IsSandbox && x.ReferenceId != null).Select(x => x.ReferenceId!).Take(1000).ToListAsync(ct);
        if (refs.Count == 0) return 0;
        return await _db.Database.SqlQuery<int>($"""
            SELECT COUNT(*)::integer AS "Value"
            FROM "JournalEntries" AS journal
            WHERE journal."TenantId" = {tenantId}
              AND NOT journal."IsSandbox"
              AND EXISTS (
                  SELECT 1
                  FROM unnest({refs.ToArray()}) AS sandbox(reference)
                  WHERE strpos(journal."Description", sandbox.reference) > 0)
            """).SingleAsync(ct);
    }

    private static FinanceControlResult Evaluate(FinanceControlDefinition definition, IReadOnlyDictionary<string, decimal> metrics)
    {
        if (!metrics.TryGetValue(definition.Metric, out var actual)) throw new InvalidOperationException($"Control metric '{definition.Metric}' is not implemented.");
        var passed = definition.Operator switch
        {
            "eq" => actual == definition.Threshold,
            "lte" => actual <= definition.Threshold,
            "gte" => actual >= definition.Threshold,
            _ => throw new InvalidOperationException($"Control operator '{definition.Operator}' is invalid.")
        };
        return new FinanceControlResult(definition.Code, definition.Title, definition.Category, definition.Severity, passed ? "PASS" : "FLAG", actual, definition.Threshold);
    }

    private static IReadOnlyList<FinanceControlDefinition> LoadLibrary()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AegisCore.Infrastructure.Finance.finance-control-library.json")
            ?? throw new InvalidOperationException("Finance control library is missing.");
        var controls = JsonSerializer.Deserialize<FinanceControlDefinition[]>(stream, JsonOptions) ?? [];
        if (controls.Length < 50 || controls.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count() != controls.Length) throw new InvalidOperationException("Finance control library must contain at least 50 unique controls.");
        return controls;
    }

    private static decimal Bool(bool value) => value ? 1m : 0m;
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string CreateToken(Guid tenantId)
    {
        var value = new byte[48];
        tenantId.TryWriteBytes(value);
        RandomNumberGenerator.Fill(value.AsSpan(16));
        return Base64Url(value);
    }

    private static bool TryReadTokenTenant(string token, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        try
        {
            var normalized = token.Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
            var value = Convert.FromBase64String(normalized);
            if (value.Length != 48) return false;
            tenantId = new Guid(value.AsSpan(0, 16));
            return tenantId != Guid.Empty;
        }
        catch (FormatException)
        {
            return false;
        }
    }
    private static string DeterministicRank(string seed, Guid id)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(hmac.ComputeHash(id.ToByteArray()));
    }
}
