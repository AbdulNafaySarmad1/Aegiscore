using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Application.Finance;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AegisCore.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks")]
public sealed class FinanceWebhookController(
    AegisDbContext db,
    ISecretService secrets,
    IConnectionMultiplexer redis,
    IFinanceModuleService finance,
    IPqcSignatureService pqc) : ControllerBase
{
    [HttpPost("{tenantId:guid}/{gatewaySlug}")]
    [RequestSizeLimit(1_048_576)]
    public async Task<IActionResult> Receive(Guid tenantId, string gatewaySlug, CancellationToken ct)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(gatewaySlug)) return Ok(new { accepted = true });
        var gateway = gatewaySlug.Trim().ToLowerInvariant();
        if (gateway.Length > 40 || gateway.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-'))) return Ok(new { accepted = true });
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.tenant_id', {tenantId:D}, false)", ct);
        var configuration = await db.PaymentGatewayConfigurations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.GatewaySlug == gateway && x.IsActive, ct);
        if (configuration is null || !string.Equals(configuration.SecretNamespace, $"/tenants/{tenantId:D}/gateways/{gateway}", StringComparison.Ordinal)) return Ok(new { accepted = true });

        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: false);
        var raw = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["X-Gateway-Signature"].ToString();
        if (raw.Length == 0 || !await IsValidSignatureAsync(configuration.SecretNamespace, raw, signature, ct)) return Ok(new { accepted = true });
        if (configuration.Environment.Equals("production", StringComparison.OrdinalIgnoreCase)
            && !await IsValidPqcSignatureAsync(configuration.SecretNamespace, raw, Request.Headers["X-Gateway-PQC-Signature"].ToString(), ct))
            return Ok(new { accepted = true });

        GatewayEnvelope envelope;
        try { envelope = Parse(raw); }
        catch (Exception) { return Ok(new { accepted = true }); }
        if (!IsConfirmed(envelope.Status)) return Ok(new { accepted = true });

        var key = $"webhook:{tenantId:D}:{gateway}:{envelope.WebhookId}";
        var database = redis.GetDatabase();
        if (!await database.StringSetAsync(key, "processing", TimeSpan.FromDays(7), When.NotExists)) return Ok(new { accepted = true });
        try
        {
            await finance.RecordGatewayPaymentAsync(tenantId, gateway, new RecordGatewayPaymentCommand(
                envelope.FeeDemandId, envelope.FeeInstallmentId, envelope.Amount, envelope.Currency,
                envelope.WebhookId, envelope.ExternalReference, envelope.SanitizedJson, configuration.Environment == "sandbox"), ct);
        }
        catch (Exception)
        {
            await database.KeyDeleteAsync(key);
        }
        return Ok(new { accepted = true });
    }

    private async Task<bool> IsValidSignatureAsync(string secretNamespace, string raw, string supplied, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(supplied)) return false;
        string secret;
        try { secret = await secrets.GetSecretAsync($"{secretNamespace}/WEBHOOK_SECRET", ct); }
        catch (Exception) { return false; }
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(raw));
        var normalized = supplied.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase) ? supplied[7..] : supplied;
        byte[] actual;
        try { actual = Convert.FromHexString(normalized); }
        catch (FormatException)
        {
            try { actual = Convert.FromBase64String(normalized); }
            catch (FormatException) { return false; }
        }
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private async Task<bool> IsValidPqcSignatureAsync(string secretNamespace, string raw, string supplied, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(supplied)) return false;
        try
        {
            var algorithm = await secrets.GetSecretAsync($"{secretNamespace}/PQC_ALGORITHM", ct);
            var publicKey = Convert.FromBase64String(await secrets.GetSecretAsync($"{secretNamespace}/PQC_PUBLIC_KEY", ct));
            var signature = Convert.FromBase64String(supplied);
            return string.Equals(algorithm, "ML-DSA-65", StringComparison.Ordinal) && pqc.Verify(algorithm, Encoding.UTF8.GetBytes(raw), signature, publicKey);
        }
        catch (FormatException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private static GatewayEnvelope Parse(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var webhookId = RequiredString(root, "webhookId");
        var feeDemandId = RequiredGuid(root, "feeDemandId");
        var externalReference = RequiredString(root, "externalReference");
        var currency = RequiredString(root, "currency");
        if (!root.TryGetProperty("amount", out var amountValue) || !amountValue.TryGetDecimal(out var amount)) throw new FormatException();
        Guid? installment = null;
        if (root.TryGetProperty("feeInstallmentId", out var installmentValue) && installmentValue.ValueKind != JsonValueKind.Null)
            installment = installmentValue.GetGuid();
        var status = root.TryGetProperty("status", out var statusValue) ? statusValue.GetString() ?? "" : "";
        var sanitized = JsonSerializer.Serialize(new { webhookId, feeDemandId, feeInstallmentId = installment, externalReference, amount, currency, status });
        return new GatewayEnvelope(webhookId, feeDemandId, installment, amount, currency, externalReference, status, sanitized);
    }

    private static string RequiredString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()! : throw new FormatException();
    private static Guid RequiredGuid(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.TryGetGuid(out var result) && result != Guid.Empty ? result : throw new FormatException();
    private static bool IsConfirmed(string status) => status.Equals("confirmed", StringComparison.OrdinalIgnoreCase) || status.Equals("success", StringComparison.OrdinalIgnoreCase) || status.Equals("paid", StringComparison.OrdinalIgnoreCase);

    private sealed record GatewayEnvelope(string WebhookId, Guid FeeDemandId, Guid? FeeInstallmentId, decimal Amount, string Currency, string ExternalReference, string Status, string SanitizedJson);
}
