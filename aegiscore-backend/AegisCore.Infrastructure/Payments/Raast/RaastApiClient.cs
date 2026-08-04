using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Payments.Raast;

public class RaastApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RaastApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RaastInitiateResponse?> InitiatePaymentAsync(RaastInitiateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("payments/initiate", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RaastInitiateResponse>(JsonOptions, cancellationToken);
    }

    public async Task<RaastStatusResponse?> GetPaymentStatusAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"payments/{paymentId}/status", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RaastStatusResponse>(JsonOptions, cancellationToken);
    }

    public async Task<RaastTransferResponse?> TransferAsync(RaastTransferRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("transfers", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RaastTransferResponse>(JsonOptions, cancellationToken);
    }
}

public class RaastInitiateRequest
{
    public string DebtorIban { get; set; } = "";
    public string CreditorIban { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR";
    public string? Reference { get; set; }
}

public class RaastInitiateResponse
{
    public string PaymentId { get; set; } = "";
    public string Status { get; set; } = "";
}

public class RaastStatusResponse
{
    public string PaymentId { get; set; } = "";
    public string Status { get; set; } = "";
}

public class RaastTransferRequest
{
    public string FromIban { get; set; } = "";
    public string ToIban { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR";
}

public class RaastTransferResponse
{
    public string TransferId { get; set; } = "";
    public string Status { get; set; } = "";
}

public interface IRaastPaymentService
{
    Task<RaastInitiateResponse?> InitiateAsync(RaastInitiateRequest request, CancellationToken cancellationToken = default);
}

public class RaastPaymentService : IRaastPaymentService
{
    private readonly RaastApiClient _client;

    public RaastPaymentService(RaastApiClient client)
    {
        _client = client;
    }

    public Task<RaastInitiateResponse?> InitiateAsync(RaastInitiateRequest request, CancellationToken cancellationToken = default)
        => _client.InitiatePaymentAsync(request, cancellationToken);
}

public static class RaastServiceExtensions
{
    public static IServiceCollection AddRaastPayments(this IServiceCollection services)
    {
        services.AddScoped<IRaastPaymentService, RaastPaymentService>();
        return services;
    }
}
