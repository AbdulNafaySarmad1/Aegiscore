using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Application.Finance;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Deployment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Finance.Gateways.PayFast;

public sealed record PayFastCheckoutStart(Guid SessionId, string BasketId, string ActionUrl, IReadOnlyDictionary<string, string> Fields, decimal Amount, string Currency, DateTime ExpiresAt, bool IsSandbox);

public sealed record FeeCheckoutView(Guid SessionId, string BasketId, Guid FeeDemandId, string DemandNumber, decimal Amount, string Currency, string Status, string DemandStatus, string? TransactionId, string? GatewayMessage, bool IsSandbox, DateTime CreatedAt, DateTime ExpiresAt, DateTime? CompletedAt);

public enum PayFastNotificationOutcome { Rejected, Paid, Failed, AlreadyFinal, NeedsReview, VerificationUnavailable }

public sealed class PayFastCheckoutException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>
/// Pays one fee demand through PayFast's hosted checkout.
///
/// AegisCore never sees card data: it asks PayFast for a one-time token, hands the student's
/// browser a form of order fields (merchant, basket, amount, return URLs — no card fields) that
/// posts straight to PayFast's own page, and learns the outcome from PayFast afterwards.
///
/// The outcome arrives twice — as an IPN to CHECKOUT_URL and as query parameters on the student's
/// return — and both go through <see cref="ProcessNotificationAsync"/>, which believes neither:
///  1. The validation hash must match SHA-256("basket_id|secured_key|merchant_id|err_code"). Only
///     PayFast and this merchant hold the secured key, so this authenticates the basket and error
///     code — and nothing else. The amount and transaction id are not covered.
///  2. The basket must be one AegisCore issued, for this tenant.
///  3. A success is then read back from PayFast's transaction status API by transaction id; the
///     record must point at the same basket, carry a success code, and carry exactly the amount
///     the attempt was opened for. Only then is the payment recorded.
/// Recording goes through <see cref="IFinanceModuleService.RecordGatewayPaymentAsync"/>, keyed on
/// the PayFast transaction id, so a retried IPN, or an IPN racing the return, records once.
///
/// Fee demand states this adds: OPEN/PARTIAL/OVERDUE → PAYMENT_PENDING → PAID/PARTIAL (success), or
/// back to exactly what it was before (failure, cancellation, expiry).
/// </summary>
public sealed class PayFastCheckoutService(
    AegisDbContext db,
    ISecretService secrets,
    IPayFastApi api,
    IFinanceModuleService finance,
    IConfiguration configuration,
    ILogger<PayFastCheckoutService> logger)
{
    public const string Slug = "payfast";
    public const string DemandPendingStatus = "PAYMENT_PENDING";
    // PayFast's published codes: 00 is "Processed OK"; some channels send 000 or 0.
    private static readonly HashSet<string> SuccessCodes = ["000", "00", "0"];

    public async Task<PayFastCheckoutStart> BeginAsync(Guid tenantId, Guid feeDemandId, Guid studentId, CancellationToken ct)
    {
        var demand = await db.FeeDemands.AsNoTracking().SingleOrDefaultAsync(d => d.TenantId == tenantId && d.Id == feeDemandId, ct)
            ?? throw new PayFastCheckoutException("not_found", "Fee demand was not found.");
        if (demand.StudentId != studentId) throw new PayFastCheckoutException("forbidden", "This fee demand belongs to another student.");
        if (demand.OutstandingAmount <= 0m || demand.Status is "PAID" or "WAIVED") throw new PayFastCheckoutException("invalid", "This fee demand is already settled.");
        if (!string.Equals(demand.Currency, "PKR", StringComparison.OrdinalIgnoreCase))
            throw new PayFastCheckoutException("invalid", "PayFast collects Pakistani rupees only; this demand is in another currency.");

        var gateway = await ActiveConfigurationAsync(tenantId, ct) ?? throw new PayFastCheckoutException("unavailable", "Online card payment is not set up for this institution.");
        await RequireLicenseAsync(tenantId, gateway, ct);
        var credentials = await CredentialsAsync(gateway, ct) ?? throw new PayFastCheckoutException("unavailable", "Online card payment is not set up for this institution.");

        var amount = decimal.Round(demand.OutstandingAmount, 2, MidpointRounding.AwayFromZero);
        var basketId = $"{demand.DemandNumber}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}";
        string? token;
        try { token = await api.GetAccessTokenAsync(credentials, basketId, amount, "PKR", ct); }
        catch (HttpRequestException) { token = null; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { token = null; }
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("PayFast did not issue an access token for tenant {TenantId}, basket {BasketId}.", tenantId, basketId);
            throw new PayFastCheckoutException("gateway_error", "PayFast could not start the payment. Please try again shortly.");
        }

        var now = DateTime.UtcNow;
        var session = await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var tracked = await db.FeeDemands.SingleAsync(d => d.TenantId == tenantId && d.Id == feeDemandId, ct);
            if (decimal.Round(tracked.OutstandingAmount, 2, MidpointRounding.AwayFromZero) != amount || tracked.Status is "PAID" or "WAIVED")
                throw new PayFastCheckoutException("invalid", "This fee demand changed while the payment was being prepared. Please try again.");
            var open = await db.FeeCheckoutSessions.Where(s => s.TenantId == tenantId && s.FeeDemandId == feeDemandId && s.Status == "PENDING").ToListAsync(ct);
            // A newer attempt replaces an older one. A late success on the older basket is still
            // honoured by ProcessNotificationAsync — PayFast took the money either way.
            foreach (var old in open) { old.Status = "SUPERSEDED"; old.CompletedAt = now; }
            var previous = tracked.Status == DemandPendingStatus
                ? open.OrderBy(s => s.CreatedAt).Select(s => s.PreviousDemandStatus).FirstOrDefault() ?? SettledStatus(tracked)
                : tracked.Status;
            var created = new FeeCheckoutSession
            {
                Id = Guid.CreateVersion7(), TenantId = tenantId, FeeDemandId = feeDemandId, StudentId = studentId, GatewaySlug = Slug,
                BasketId = basketId, Amount = amount, Currency = "PKR", Status = "PENDING", PreviousDemandStatus = previous,
                IsSandbox = credentials.IsSandbox, CreatedAt = now, ExpiresAt = now.AddMinutes(CheckoutMinutes())
            };
            db.FeeCheckoutSessions.Add(created);
            tracked.Status = DemandPendingStatus;
            db.PaymentGatewayEvents.Add(Event(tenantId, "CHECKOUT_STARTED", basketId, amount, "PENDING", credentials.IsSandbox,
                new { basketId, amount, currency = "PKR", demand = demand.DemandNumber }));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return created;
        });

        logger.LogInformation("PayFast checkout {BasketId} started for tenant {TenantId}, demand {DemandId}, amount {Amount} PKR, sandbox {IsSandbox}.",
            basketId, tenantId, feeDemandId, amount, credentials.IsSandbox);

        var profileJson = await db.StudentProfiles.AsNoTracking().Where(p => p.TenantId == tenantId && p.StudentId == studentId).Select(p => p.ProfileJson).SingleOrDefaultAsync(ct);
        var returnUrl = $"{PortalBaseUrl()}/student/finance/checkout?basket={Uri.EscapeDataString(basketId)}";
        var fields = new Dictionary<string, string>
        {
            ["MERCHANT_ID"] = credentials.MerchantId,
            ["MERCHANT_NAME"] = credentials.MerchantName,
            ["TOKEN"] = token,
            ["PROCCODE"] = "00",
            ["TRAN_TYPE"] = "ECOMM_PURCHASE",
            ["BASKET_ID"] = basketId,
            ["TXNAMT"] = PayFastApi.FormatAmount(amount),
            ["CURRENCY_CODE"] = "PKR",
            // PayFast is in Pakistan Standard Time (UTC+5, no daylight saving).
            ["ORDER_DATE"] = now.AddHours(5).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            ["TXNDESC"] = $"Fee challan {demand.DemandNumber}",
            ["SUCCESS_URL"] = $"{returnUrl}&result=success",
            ["FAILURE_URL"] = $"{returnUrl}&result=failure",
            ["CHECKOUT_URL"] = $"{PublicCallbackBaseUrl()}/api/payments/payfast/{tenantId:D}/ipn",
            ["VERSION"] = "AEGISCORE-1.0"
        };
        var mobile = ProfileField(profileJson, "registeredMobile", "mobile", "phone");
        if (mobile is not null) fields["CUSTOMER_MOBILE_NO"] = mobile;
        var email = ProfileField(profileJson, "email", "personalEmail");
        if (email is not null) fields["CUSTOMER_EMAIL_ADDRESS"] = email;

        return new PayFastCheckoutStart(session.Id, basketId, api.PostTransactionUrl(credentials.IsSandbox), fields, amount, "PKR", session.ExpiresAt, credentials.IsSandbox);
    }

    /// <summary>
    /// Handles PayFast's word on a basket, from the IPN or from the student's return. Never trusts
    /// it: see the class summary. <paramref name="requiredStudentId"/> is set on the return path so
    /// a signed-in student can only report on their own baskets.
    /// </summary>
    public async Task<PayFastNotificationOutcome> ProcessNotificationAsync(Guid tenantId, IReadOnlyDictionary<string, string> fields, string channel, Guid? requiredStudentId, CancellationToken ct)
    {
        var basketId = Field(fields, "basket_id", "BASKET_ID", "order_no", "ORDER_NO");
        var errorCode = Field(fields, "err_code", "ERR_CODE");
        var receivedHash = Field(fields, "validation_hash", "VALIDATION_HASH");
        var transactionId = Field(fields, "transaction_id", "TRANSACTION_ID");
        if (basketId is null || errorCode is null || receivedHash is null || basketId.Length > 60 || errorCode.Length > 10 || receivedHash.Length > 128 || transactionId?.Length > 90)
        {
            logger.LogWarning("PayFast {Channel} for tenant {TenantId} was missing required fields; ignored.", channel, tenantId);
            return PayFastNotificationOutcome.Rejected;
        }

        var gateway = await ActiveConfigurationAsync(tenantId, ct);
        var credentials = gateway is null ? null : await CredentialsAsync(gateway, ct);
        if (credentials is null)
        {
            logger.LogWarning("PayFast {Channel} for tenant {TenantId} arrived but PayFast is not configured; ignored.", channel, tenantId);
            return PayFastNotificationOutcome.Rejected;
        }
        if (!ValidationHashMatches(credentials, basketId, errorCode, receivedHash))
        {
            logger.LogWarning("PayFast {Channel} for tenant {TenantId} failed validation-hash verification; ignored.", channel, tenantId);
            return PayFastNotificationOutcome.Rejected;
        }

        var session = await db.FeeCheckoutSessions.SingleOrDefaultAsync(s => s.TenantId == tenantId && s.GatewaySlug == Slug && s.BasketId == basketId, ct);
        if (session is null || requiredStudentId is not null && session.StudentId != requiredStudentId)
        {
            logger.LogWarning("PayFast {Channel} for tenant {TenantId} named basket {BasketId}, which AegisCore did not issue to this caller; ignored.", channel, tenantId, basketId);
            return PayFastNotificationOutcome.Rejected;
        }
        if (session.Status == "PAID") return PayFastNotificationOutcome.AlreadyFinal;

        if (!SuccessCodes.Contains(errorCode))
        {
            if (session.Status != "PENDING") return PayFastNotificationOutcome.AlreadyFinal;
            session.Status = "FAILED";
            session.GatewayErrorCode = errorCode;
            session.GatewayMessage = Clean(Field(fields, "err_msg", "ERR_MSG")) ?? "The payment was not completed.";
            session.CompletedAt = DateTime.UtcNow;
            await RestoreDemandAsync(session, ct);
            db.PaymentGatewayEvents.Add(Event(tenantId, "CHECKOUT_FAILED", basketId, session.Amount, "FAILED", session.IsSandbox, new { basketId, errorCode, channel }));
            await db.SaveChangesAsync(ct);
            logger.LogInformation("PayFast checkout {BasketId} for tenant {TenantId} failed with code {ErrorCode} ({Channel}); demand restored.", basketId, tenantId, errorCode, channel);
            return PayFastNotificationOutcome.Failed;
        }

        if (transactionId is null)
        {
            logger.LogWarning("PayFast {Channel} success for basket {BasketId} carried no transaction id; cannot verify.", channel, basketId);
            return PayFastNotificationOutcome.Rejected;
        }
        return await VerifyAndSettleAsync(session, credentials, transactionId, channel, ct);
    }

    public async Task<FeeCheckoutView> GetAsync(Guid tenantId, string basketId, Guid studentId, CancellationToken ct)
    {
        var session = await OwnSessionAsync(tenantId, basketId, studentId, ct);
        if (session.Status == "PENDING" && session.ExpiresAt <= DateTime.UtcNow) await ExpireAsync(session, ct);
        return await ViewAsync(session, ct);
    }

    /// <summary>The student came back from PayFast without paying, or gave up. A late verified success is still honoured.</summary>
    public async Task<FeeCheckoutView> CancelAsync(Guid tenantId, string basketId, Guid studentId, CancellationToken ct)
    {
        var session = await OwnSessionAsync(tenantId, basketId, studentId, ct);
        if (session.Status == "PENDING")
        {
            session.Status = "CANCELLED";
            session.GatewayMessage = "Cancelled before PayFast confirmed a payment.";
            session.CompletedAt = DateTime.UtcNow;
            await RestoreDemandAsync(session, ct);
            db.PaymentGatewayEvents.Add(Event(tenantId, "CHECKOUT_CANCELLED", basketId, session.Amount, "CANCELLED", session.IsSandbox, new { basketId }));
            await db.SaveChangesAsync(ct);
            logger.LogInformation("PayFast checkout {BasketId} for tenant {TenantId} cancelled by the student; demand restored.", basketId, tenantId);
        }
        return await ViewAsync(session, ct);
    }

    /// <summary>
    /// Closes attempts nobody finished, so no demand sits in PAYMENT_PENDING for ever. An attempt
    /// PayFast already named a transaction for is re-checked with PayFast first.
    /// </summary>
    public async Task<int> ExpireStaleAsync(Guid tenantId, DateTime now, CancellationToken ct)
    {
        var stale = await db.FeeCheckoutSessions.Where(s => s.TenantId == tenantId && s.Status == "PENDING" && s.ExpiresAt <= now)
            .OrderBy(s => s.ExpiresAt).Take(200).Select(s => s.Id).ToListAsync(ct);
        var closed = 0;
        foreach (var id in stale)
        {
            var session = await db.FeeCheckoutSessions.SingleAsync(s => s.Id == id, ct);
            if (session.Status != "PENDING") continue;
            if (session.GatewayTransactionId is not null)
            {
                var gateway = await ActiveConfigurationAsync(tenantId, ct);
                var credentials = gateway is null ? null : await CredentialsAsync(gateway, ct);
                if (credentials is not null && await VerifyAndSettleAsync(session, credentials, session.GatewayTransactionId, "expiry-recheck", ct) != PayFastNotificationOutcome.VerificationUnavailable)
                {
                    session = await db.FeeCheckoutSessions.SingleAsync(s => s.Id == id, ct);
                    if (session.Status != "PENDING") continue;
                }
            }
            await ExpireAsync(session, ct);
            closed++;
        }
        return closed;
    }

    private async Task<PayFastNotificationOutcome> VerifyAndSettleAsync(FeeCheckoutSession session, PayFastCredentials credentials, string transactionId, string channel, CancellationToken ct)
    {
        if (session.GatewayTransactionId is null && session.Status == "PENDING")
        {
            // Kept so the expiry sweep can re-check with PayFast if the lookup below is unavailable.
            session.GatewayTransactionId = transactionId;
            await db.SaveChangesAsync(ct);
        }

        PayFastTransactionRecord? record;
        try { record = await api.GetTransactionAsync(credentials, transactionId, ct); }
        catch (HttpRequestException) { record = null; }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { record = null; }
        if (record is null)
        {
            logger.LogWarning("PayFast transaction lookup for basket {BasketId} was unavailable ({Channel}); left as it was.", session.BasketId, channel);
            return PayFastNotificationOutcome.VerificationUnavailable;
        }
        if (!string.Equals(record.BasketId, session.BasketId, StringComparison.Ordinal))
        {
            logger.LogWarning("The PayFast transaction named for basket {BasketId} belongs to a different basket ({Channel}); ignored.", session.BasketId, channel);
            return PayFastNotificationOutcome.Rejected;
        }
        if (record.StatusCode is null || !SuccessCodes.Contains(record.StatusCode))
        {
            logger.LogWarning("PayFast reports basket {BasketId} as not successful (status {StatusCode}) despite a success notice ({Channel}); not recorded.", session.BasketId, record.StatusCode, channel);
            return PayFastNotificationOutcome.Rejected;
        }
        var reference = record.TransactionId ?? transactionId;
        if (record.Amount is null || decimal.Round(record.Amount.Value, 2, MidpointRounding.AwayFromZero) != session.Amount)
        {
            // PayFast took money, but not the amount this attempt was opened for. A person decides.
            var confirmed = record.Amount?.ToString("0.00", CultureInfo.InvariantCulture) ?? "no";
            return await NeedsReviewAsync(session, reference, $"PayFast confirmed {confirmed} amount against an expected {session.Amount.ToString("0.00", CultureInfo.InvariantCulture)}.", ct);
        }

        var webhookId = $"payfast:{reference}";
        FinancePayment payment;
        try
        {
            payment = await finance.RecordGatewayPaymentAsync(session.TenantId, Slug, new RecordGatewayPaymentCommand(
                session.FeeDemandId, null, session.Amount, session.Currency, webhookId, reference,
                JsonSerializer.Serialize(new { basketId = session.BasketId, transactionId = reference, amount = session.Amount, currency = session.Currency, channel }),
                session.IsSandbox), ct);
        }
        catch (BusinessRuleException ex)
        {
            // Verified money that cannot be applied — typically the demand was settled another way meanwhile.
            db.ChangeTracker.Clear();
            var reloaded = await db.FeeCheckoutSessions.SingleAsync(s => s.Id == session.Id, ct);
            return await NeedsReviewAsync(reloaded, reference, $"Paid at PayFast but not applied: {ex.Message}", ct);
        }
        catch (DbUpdateException ex)
        {
            // Most likely the IPN and the student's return raced and the other one recorded it.
            db.ChangeTracker.Clear();
            payment = await db.FinancePayments.SingleOrDefaultAsync(p => p.TenantId == session.TenantId && p.WebhookId == webhookId, ct)
                ?? throw new InvalidOperationException("The PayFast payment could not be recorded.", ex);
        }

        var current = await db.FeeCheckoutSessions.SingleAsync(s => s.Id == session.Id, ct);
        if (current.Status != "PAID")
        {
            current.Status = "PAID";
            current.GatewayTransactionId = reference;
            current.GatewayErrorCode = record.StatusCode;
            current.GatewayMessage = null;
            current.FinancePaymentId = payment.Id;
            current.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        logger.LogInformation("PayFast checkout {BasketId} for tenant {TenantId} verified and recorded as payment {PaymentId}, transaction {TransactionId} ({Channel}).",
            session.BasketId, session.TenantId, payment.Id, reference, channel);
        return PayFastNotificationOutcome.Paid;
    }

    private async Task<PayFastNotificationOutcome> NeedsReviewAsync(FeeCheckoutSession session, string reference, string reason, CancellationToken ct)
    {
        session.Status = "NEEDS_REVIEW";
        session.GatewayTransactionId = reference;
        session.GatewayMessage = Clean(reason);
        session.CompletedAt = DateTime.UtcNow;
        await RestoreDemandAsync(session, ct);
        db.PaymentGatewayEvents.Add(Event(session.TenantId, "CHECKOUT_NEEDS_REVIEW", reference, session.Amount, "NEEDS_REVIEW", session.IsSandbox, new { basketId = session.BasketId, transactionId = reference }));
        await db.SaveChangesAsync(ct);
        logger.LogWarning("PayFast checkout {BasketId} for tenant {TenantId} needs finance review: {Reason}", session.BasketId, session.TenantId, session.GatewayMessage);
        return PayFastNotificationOutcome.NeedsReview;
    }

    private async Task ExpireAsync(FeeCheckoutSession session, CancellationToken ct)
    {
        session.Status = "EXPIRED";
        session.GatewayMessage = "PayFast did not confirm a payment before this attempt expired.";
        session.CompletedAt = DateTime.UtcNow;
        await RestoreDemandAsync(session, ct);
        db.PaymentGatewayEvents.Add(Event(session.TenantId, "CHECKOUT_EXPIRED", session.BasketId, session.Amount, "EXPIRED", session.IsSandbox, new { basketId = session.BasketId }));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("PayFast checkout {BasketId} for tenant {TenantId} expired; demand restored.", session.BasketId, session.TenantId);
    }

    /// <summary>Puts the demand back to its pre-attempt status, unless something else has moved it on or another attempt is live.</summary>
    private async Task RestoreDemandAsync(FeeCheckoutSession session, CancellationToken ct)
    {
        var demand = await db.FeeDemands.SingleOrDefaultAsync(d => d.TenantId == session.TenantId && d.Id == session.FeeDemandId, ct);
        if (demand is null || demand.Status != DemandPendingStatus) return;
        var anotherLive = await db.FeeCheckoutSessions.AnyAsync(s => s.TenantId == session.TenantId && s.FeeDemandId == session.FeeDemandId && s.Id != session.Id && s.Status == "PENDING", ct);
        if (!anotherLive) demand.Status = session.PreviousDemandStatus;
    }

    private async Task<FeeCheckoutSession> OwnSessionAsync(Guid tenantId, string basketId, Guid studentId, CancellationToken ct)
    {
        var session = await db.FeeCheckoutSessions.SingleOrDefaultAsync(s => s.TenantId == tenantId && s.GatewaySlug == Slug && s.BasketId == basketId, ct);
        return session is not null && session.StudentId == studentId ? session : throw new PayFastCheckoutException("not_found", "That payment attempt was not found.");
    }

    private async Task<FeeCheckoutView> ViewAsync(FeeCheckoutSession session, CancellationToken ct)
    {
        var demand = await db.FeeDemands.AsNoTracking().Where(d => d.TenantId == session.TenantId && d.Id == session.FeeDemandId)
            .Select(d => new { d.DemandNumber, d.Status }).SingleAsync(ct);
        return new FeeCheckoutView(session.Id, session.BasketId, session.FeeDemandId, demand.DemandNumber, session.Amount, session.Currency, session.Status, demand.Status,
            session.GatewayTransactionId, session.GatewayMessage, session.IsSandbox, session.CreatedAt, session.ExpiresAt, session.CompletedAt);
    }

    private Task<PaymentGatewayConfiguration?> ActiveConfigurationAsync(Guid tenantId, CancellationToken ct)
        // Explicit tenant predicate: the IPN is anonymous and carries its tenant in the URL.
        => db.PaymentGatewayConfigurations.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.GatewaySlug == Slug && x.IsActive, ct);

    private async Task RequireLicenseAsync(Guid tenantId, PaymentGatewayConfiguration gateway, CancellationToken ct)
    {
        var license = await db.Licenses.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (license is null || license.Status != LicenseStatus.Active || license.ExpiresAt is not null && license.ExpiresAt <= DateTime.UtcNow)
            throw new PayFastCheckoutException("unavailable", "An active licence is required to take online payments.");
        if (license.Edition == LicenseEdition.Community && !IsSandbox(gateway))
            throw new PayFastCheckoutException("unavailable", "The Community edition takes PayFast payments in sandbox only.");
    }

    private async Task<PayFastCredentials?> CredentialsAsync(PaymentGatewayConfiguration gateway, CancellationToken ct)
    {
        if (!string.Equals(gateway.SecretNamespace, $"/tenants/{gateway.TenantId:D}/gateways/{Slug}", StringComparison.Ordinal)) return null;
        try
        {
            var merchantId = await secrets.GetSecretAsync($"{gateway.SecretNamespace}/MERCHANT_ID", ct);
            var securedKey = await secrets.GetSecretAsync($"{gateway.SecretNamespace}/SECURED_KEY", ct);
            var merchantName = await secrets.GetSecretAsync($"{gateway.SecretNamespace}/MERCHANT_NAME", ct);
            if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(securedKey) || string.IsNullOrWhiteSpace(merchantName)) return null;
            return new PayFastCredentials(merchantId.Trim(), securedKey.Trim(), merchantName.Trim(), IsSandbox(gateway));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            // Deliberately no exception detail: a secret-store error message can echo the secret path or value.
            logger.LogWarning("PayFast credentials for tenant {TenantId} could not be read from the secret store.", gateway.TenantId);
            return null;
        }
    }

    /// <summary>PayFast's validation hash: lowercase hex SHA-256 of "basket_id|secured_key|merchant_id|err_code".</summary>
    public static string ValidationHash(PayFastCredentials credentials, string basketId, string errorCode)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{basketId}|{credentials.SecuredKey}|{credentials.MerchantId}|{errorCode}")));

    private static bool ValidationHashMatches(PayFastCredentials credentials, string basketId, string errorCode, string received)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(ValidationHash(credentials, basketId, errorCode)),
            Encoding.ASCII.GetBytes(received.Trim().ToLowerInvariant()));

    private static bool IsSandbox(PaymentGatewayConfiguration gateway) => !gateway.Environment.Equals("production", StringComparison.OrdinalIgnoreCase);

    private static string SettledStatus(FeeDemand demand)
        => demand.OutstandingAmount <= 0m ? "PAID" : demand.PaidAmount > 0m || demand.ScholarshipDeduction > 0m ? "PARTIAL" : "OPEN";

    private int CheckoutMinutes() => Math.Clamp(configuration.GetValue("PaymentGateways:payfast:CheckoutMinutes", 30), 5, 240);

    private string PortalBaseUrl() => (configuration["APP_URL"] ?? "http://localhost:3000").TrimEnd('/');

    /// <summary>Where PayFast can reach the IPN. Defaults to the portal origin, which forwards /api to the API.</summary>
    private string PublicCallbackBaseUrl()
    {
        var configured = configuration["PaymentGateways:payfast:PublicCallbackBaseUrl"];
        return (string.IsNullOrWhiteSpace(configured) ? PortalBaseUrl() : configured).TrimEnd('/');
    }

    private static PaymentGatewayEvent Event(Guid tenantId, string type, string reference, decimal amount, string status, bool sandbox, object sanitized) => new()
    {
        Id = Guid.CreateVersion7(), TenantId = tenantId, GatewaySlug = Slug, EventType = type, ReferenceId = reference,
        Amount = amount, Currency = "PKR", Status = status, IsSandbox = sandbox, SanitizedPayloadJson = JsonSerializer.Serialize(sanitized)
    };

    private static string? Field(IReadOnlyDictionary<string, string> fields, params string[] names)
    {
        foreach (var name in names)
            if (fields.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
        return null;
    }

    private static string? ProfileField(string? profileJson, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(profileJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(profileJson);
            foreach (var name in names)
                if (document.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
                    return value.GetString()!.Trim();
        }
        catch (JsonException) { }
        return null;
    }

    private static string? Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var cleaned = new string(text.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return cleaned.Length <= 200 ? cleaned : cleaned[..200];
    }
}
