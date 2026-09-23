using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Finance.Gateways.PayFast;

/// <summary>Credentials for one tenant's PayFast merchant account, read from the secret store per call.</summary>
public sealed record PayFastCredentials(string MerchantId, string SecuredKey, string MerchantName, bool IsSandbox);

/// <summary>
/// PayFast's authoritative record of a transaction, as returned by its transaction status API.
/// Only the fields AegisCore decides on are kept; the raw body is never stored or logged.
/// </summary>
public sealed record PayFastTransactionRecord(string? BasketId, string? StatusCode, decimal? Amount, string? TransactionId);

/// <summary>
/// The three calls AegisCore makes to PayFast (Pakistan, gopayfast.com / APPS). Every one is
/// server-to-server; the student's browser only ever talks to PayFast's hosted payment page.
/// </summary>
public interface IPayFastApi
{
    /// <summary>The URL the student's browser posts the checkout form to (PayFast's hosted page).</summary>
    string PostTransactionUrl(bool sandbox);

    /// <summary>A one-time token that authorises exactly this basket and amount.</summary>
    Task<string?> GetAccessTokenAsync(PayFastCredentials credentials, string basketId, decimal amount, string currency, CancellationToken ct);

    /// <summary>Reads a transaction back from PayFast, so the amount is never taken from a callback.</summary>
    Task<PayFastTransactionRecord?> GetTransactionAsync(PayFastCredentials credentials, string transactionId, CancellationToken ct);
}

/// <summary>
/// Hosts: UAT <c>https://ipguat.apps.net.pk</c>, live <c>https://ipg1.apps.net.pk</c>; the
/// transaction status API is on port 8443 of the same hosts. Overridable under
/// <c>PaymentGateways:payfast</c> so a deployment can follow PayFast if it moves them.
/// </summary>
public sealed class PayFastApi(IHttpClientFactory clients, IConfiguration configuration) : IPayFastApi
{
    public const string HttpClientName = "finance-gateways";

    public string PostTransactionUrl(bool sandbox) => $"{PaymentBase(sandbox)}/Ecommerce/api/Transaction/PostTransaction";

    public async Task<string?> GetAccessTokenAsync(PayFastCredentials credentials, string basketId, decimal amount, string currency, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{PaymentBase(credentials.IsSandbox)}/Ecommerce/api/Transaction/GetAccessToken")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["MERCHANT_ID"] = credentials.MerchantId,
                ["SECURED_KEY"] = credentials.SecuredKey,
                ["BASKET_ID"] = basketId,
                ["TXNAMT"] = FormatAmount(amount),
                ["CURRENCY_CODE"] = currency
            })
        };
        using var response = await clients.CreateClient(HttpClientName).SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        using var document = await ParseAsync(response, ct);
        return document is null ? null : Field(document.RootElement, "ACCESS_TOKEN", "access_token", "token");
    }

    public async Task<PayFastTransactionRecord?> GetTransactionAsync(PayFastCredentials credentials, string transactionId, CancellationToken ct)
    {
        var client = clients.CreateClient(HttpClientName);
        // The status API has its own bearer token; the payment access token does not work there.
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{StatusBase(credentials.IsSandbox)}/api/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["merchant_id"] = credentials.MerchantId,
                ["grant_type"] = "client_credentials",
                ["secured_key"] = credentials.SecuredKey
            })
        };
        using var tokenResponse = await client.SendAsync(tokenRequest, ct);
        if (!tokenResponse.IsSuccessStatusCode) return null;
        string? bearer;
        using (var tokenDocument = await ParseAsync(tokenResponse, ct))
            bearer = tokenDocument is null ? null : Field(tokenDocument.RootElement, "token", "access_token", "ACCESS_TOKEN");
        if (string.IsNullOrWhiteSpace(bearer)) return null;

        using var lookup = new HttpRequestMessage(HttpMethod.Get, $"{StatusBase(credentials.IsSandbox)}/api/transaction/{Uri.EscapeDataString(transactionId)}");
        lookup.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        using var response = await client.SendAsync(lookup, ct);
        if (!response.IsSuccessStatusCode) return null;
        using var document = await ParseAsync(response, ct);
        if (document is null) return null;
        var root = document.RootElement;
        // PayFast answers 200 even for an unknown transaction; the record's own fields carry the outcome.
        var amountText = Field(root, "transaction_amount", "purchase_amount", "TXNAMT", "amount", "AMOUNT", "merchant_amount");
        decimal? amount = decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        return new PayFastTransactionRecord(
            Field(root, "basket_id", "BASKET_ID"),
            Field(root, "status_code", "STATUS_CODE", "err_code", "ERR_CODE", "code", "CODE"),
            amount,
            Field(root, "transaction_id", "TRANSACTION_ID"));
    }

    public static string FormatAmount(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);

    private string PaymentBase(bool sandbox) => Host(sandbox ? "SandboxBaseUrl" : "LiveBaseUrl", sandbox ? "https://ipguat.apps.net.pk" : "https://ipg1.apps.net.pk");
    private string StatusBase(bool sandbox) => Host(sandbox ? "SandboxStatusBaseUrl" : "LiveStatusBaseUrl", sandbox ? "https://ipguat.apps.net.pk:8443" : "https://ipg1.apps.net.pk:8443");

    private string Host(string key, string fallback)
    {
        var value = configuration[$"PaymentGateways:payfast:{key}"];
        var chosen = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (!Uri.TryCreate(chosen, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("PayFast endpoints must be HTTPS.");
        return chosen.TrimEnd('/');
    }

    private static async Task<JsonDocument?> ParseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (body.Length is 0 or > 65536) return null;
        try
        {
            var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object) return document;
            document.Dispose();
            return null;
        }
        catch (JsonException) { return null; }
    }

    private static string? Field(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())) return value.GetString()!.Trim();
            if (value.ValueKind == JsonValueKind.Number) return value.GetRawText();
        }
        return null;
    }
}
