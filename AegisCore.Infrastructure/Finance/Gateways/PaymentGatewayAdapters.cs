using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Finance.Gateways;

public sealed record GatewayRtpRequest(Guid FeeDemandId, Guid? FeeInstallmentId, decimal Amount, string Currency, string PayerReference, string IdempotencyKey);
public sealed record GatewayRtpResult(string ReferenceId, DateTime ExpiresAt, string? DeepLink, bool IsSandbox, string SanitizedResponseJson);

public interface IPaymentGatewayAdapter
{
    string Slug { get; }
    Task<GatewayRtpResult> SendRtpAsync(Guid tenantId, string secretNamespace, GatewayRtpRequest request, CancellationToken ct);
}

public sealed class PaymentGatewayRouter(AegisDbContext db, IEnumerable<IPaymentGatewayAdapter> adapters, SandboxPaymentGatewayAdapter sandbox)
{
    private readonly IReadOnlyDictionary<string, IPaymentGatewayAdapter> adapterBySlug = adapters.ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);

    public async Task<GatewayRtpResult> SendRtpAsync(Guid tenantId, string slug, GatewayRtpRequest request, CancellationToken ct)
    {
        var license = await db.Licenses.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("An active tenant license is required.");
        if (license.Status != LicenseStatus.Active || license.ExpiresAt is not null && license.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("An active tenant license is required.");
        var configuration = await db.PaymentGatewayConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.GatewaySlug == slug && x.IsActive && x.HealthStatus == "ok", ct)
            ?? throw new InvalidOperationException("The selected gateway is not configured and healthy.");
        if (license.Edition == LicenseEdition.Community || configuration.Environment.Equals("sandbox", StringComparison.OrdinalIgnoreCase))
            return await sandbox.SendRtpAsync(tenantId, configuration.SecretNamespace, request, ct);
        if (!adapterBySlug.TryGetValue(slug, out var adapter)) throw new NotSupportedException("The selected live payment gateway is not supported.");
        return await adapter.SendRtpAsync(tenantId, configuration.SecretNamespace, request, ct);
    }
}

public sealed class SandboxPaymentGatewayAdapter : IPaymentGatewayAdapter
{
    public string Slug => "sandbox";
    public async Task<GatewayRtpResult> SendRtpAsync(Guid tenantId, string secretNamespace, GatewayRtpRequest request, CancellationToken ct)
    {
        await Task.Delay(800, ct);
        var reference = $"SANDBOX-{Guid.CreateVersion7():N}";
        return new(reference, DateTime.UtcNow.AddMinutes(30), $"aegiscore-sandbox://payment/{reference}", true, JsonSerializer.Serialize(new { reference, status = "pending" }));
    }
}

public abstract class HttpPaymentGatewayAdapter(IHttpClientFactory clients, ISecretService secrets, IConfiguration configuration) : IPaymentGatewayAdapter
{
    protected ISecretService Secrets { get; } = secrets;
    public abstract string Slug { get; }
    protected abstract Task<HttpRequestMessage> CreateRequestAsync(string secretNamespace, GatewayRtpRequest request, CancellationToken ct);

    public async Task<GatewayRtpResult> SendRtpAsync(Guid tenantId, string secretNamespace, GatewayRtpRequest request, CancellationToken ct)
    {
        var baseUrl = configuration[$"PaymentGateways:{Slug}:BaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("Payment gateway base URL must be configured with HTTPS.");
        using var message = await CreateRequestAsync(secretNamespace, request, ct);
        message.RequestUri = new Uri(uri, message.RequestUri ?? throw new InvalidOperationException("Payment gateway request path is missing."));
        message.Headers.TryAddWithoutValidation("Idempotency-Key", request.IdempotencyKey);
        using var response = await clients.CreateClient("finance-gateways").SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Payment gateway rejected the request with status {(int)response.StatusCode}.", null, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var reference = FirstString(root, "id", "payment_id", "request_id", "transfer_id", "bill_id")
            ?? throw new InvalidOperationException("Payment gateway response did not include a transaction reference.");
        var deepLink = FirstString(root, "deep_link", "url", "checkout_url", "payment_url");
        return new(reference, DateTime.UtcNow.AddMinutes(30), deepLink, false, JsonSerializer.Serialize(new { reference, status = FirstString(root, "status") ?? "pending" }));
    }

    protected async Task<string> SecretAsync(string secretNamespace, string name, CancellationToken ct)
        => await Secrets.GetSecretAsync($"{secretNamespace}/{name}", ct);

    protected static StringContent JsonBody(object value)
        => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static string? FirstString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
            if (root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String) return property.GetString();
        return null;
    }
}

public sealed class RaastPaymentGatewayAdapter(IHttpClientFactory clients, ISecretService secrets, IConfiguration configuration) : HttpPaymentGatewayAdapter(clients, secrets, configuration)
{
    public override string Slug => "raast";
    protected override async Task<HttpRequestMessage> CreateRequestAsync(string ns, GatewayRtpRequest request, CancellationToken ct)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "rtp/requests") { Content = JsonBody(new { amount = request.Amount, currency = request.Currency, debtor_alias = request.PayerReference, creditor_iban = await SecretAsync(ns, "IBAN", ct), reference = request.FeeDemandId }) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await SecretAsync(ns, "CLIENT_SECRET", ct));
        message.Headers.TryAddWithoutValidation("X-Client-Id", await SecretAsync(ns, "CLIENT_ID", ct));
        message.Headers.TryAddWithoutValidation("X-Merchant-Id", await SecretAsync(ns, "MERCHANT_ID", ct));
        return message;
    }
}

public sealed class StripePaymentGatewayAdapter(IHttpClientFactory clients, ISecretService secrets, IConfiguration configuration) : HttpPaymentGatewayAdapter(clients, secrets, configuration)
{
    public override string Slug => "stripe";
    protected override async Task<HttpRequestMessage> CreateRequestAsync(string ns, GatewayRtpRequest request, CancellationToken ct)
    {
        var minor = checked((long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero));
        var message = new HttpRequestMessage(HttpMethod.Post, "v1/payment_intents") { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["amount"] = minor.ToString(System.Globalization.CultureInfo.InvariantCulture), ["currency"] = request.Currency.ToLowerInvariant(), ["metadata[fee_demand_id]"] = request.FeeDemandId.ToString("D") }) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await SecretAsync(ns, "SECRET_KEY", ct));
        return message;
    }
}

public sealed class PlaidAchPaymentGatewayAdapter(IHttpClientFactory clients, ISecretService secrets, IConfiguration configuration) : HttpPaymentGatewayAdapter(clients, secrets, configuration)
{
    public override string Slug => "plaid-ach";
    protected override async Task<HttpRequestMessage> CreateRequestAsync(string ns, GatewayRtpRequest request, CancellationToken ct)
        => new(HttpMethod.Post, "transfer/create") { Content = JsonBody(new { client_id = await SecretAsync(ns, "CLIENT_ID", ct), secret = await SecretAsync(ns, "SECRET_KEY", ct), idempotency_key = request.IdempotencyKey, amount = request.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), description = $"Fee demand {request.FeeDemandId:D}", user = new { legal_name = request.PayerReference } }) };
}

public sealed class GoCardlessPaymentGatewayAdapter(IHttpClientFactory clients, ISecretService secrets, IConfiguration configuration) : HttpPaymentGatewayAdapter(clients, secrets, configuration)
{
    public override string Slug => "gocardless";
    protected override async Task<HttpRequestMessage> CreateRequestAsync(string ns, GatewayRtpRequest request, CancellationToken ct)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "payments") { Content = JsonBody(new { payments = new { amount = checked((long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero)), currency = request.Currency, links = new { mandate = request.PayerReference }, metadata = new { fee_demand_id = request.FeeDemandId } } }) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await SecretAsync(ns, "ACCESS_TOKEN", ct));
        message.Headers.TryAddWithoutValidation("GoCardless-Version", "2015-07-06");
        return message;
    }
}

public sealed class BillplzFpxPaymentGatewayAdapter(IHttpClientFactory clients, ISecretService secrets, IConfiguration configuration) : HttpPaymentGatewayAdapter(clients, secrets, configuration)
{
    public override string Slug => "billplz-fpx";
    protected override async Task<HttpRequestMessage> CreateRequestAsync(string ns, GatewayRtpRequest request, CancellationToken ct)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "api/v3/bills") { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["collection_id"] = request.PayerReference, ["description"] = $"Fee demand {request.FeeDemandId:D}", ["amount"] = checked((long)decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero)).ToString(), ["name"] = "AegisCore student", ["email"] = "payments@aegiscore.invalid" }) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{await SecretAsync(ns, "API_KEY", ct)}:")));
        return message;
    }
}

public sealed class DuitNowPaymentGatewayAdapter(IHttpClientFactory clients, ISecretService secrets, IConfiguration configuration) : HttpPaymentGatewayAdapter(clients, secrets, configuration)
{
    public override string Slug => "duitnow-qr";
    protected override async Task<HttpRequestMessage> CreateRequestAsync(string ns, GatewayRtpRequest request, CancellationToken ct)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "v1/qr/dynamic") { Content = JsonBody(new { merchant_id = await SecretAsync(ns, "MERCHANT_ID", ct), amount = request.Amount, currency = request.Currency, reference = request.FeeDemandId, expiry_seconds = 1800 }) };
        message.Headers.TryAddWithoutValidation("X-Client-Id", await SecretAsync(ns, "CLIENT_ID", ct));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await SecretAsync(ns, "CLIENT_SECRET", ct));
        return message;
    }
}
