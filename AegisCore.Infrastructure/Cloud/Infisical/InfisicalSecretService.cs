using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AegisCore.Contracts.Abstractions.Cloud;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Cloud.Infisical;

public sealed class InfisicalSecretService(IHttpClientFactory clients, IConfiguration configuration) : ISecretService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/api/v3/secrets/raw/{Uri.EscapeDataString(secretName)}", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SecretResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Secret provider returned an empty response.");
        return payload.SecretValue ?? throw new InvalidOperationException("Secret provider returned no value.");
    }

    public async Task SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName) || string.IsNullOrEmpty(value)) throw new ArgumentException("Secret name and value are required.");
        var slash = secretName.LastIndexOf('/');
        var path = slash < 0 ? "/" : secretName[..slash];
        var key = slash < 0 ? secretName : secretName[(slash + 1)..];
        var body = new { workspaceId = Required("INFISICAL_WORKSPACE_ID"), environment = EnvironmentName(), secretPath = path, secretKey = key, secretValue = value };
        using var response = await SendAsync(HttpMethod.Post, "/api/v3/secrets", body, cancellationToken);
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode != System.Net.HttpStatusCode.Conflict) response.EnsureSuccessStatusCode();
        using var update = await SendAsync(HttpMethod.Patch, $"/api/v3/secrets/{Uri.EscapeDataString(key)}", body, cancellationToken);
        update.EnsureSuccessStatusCode();
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v3/secrets/{Uri.EscapeDataString(secretName)}", null, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/api/v3/secrets/raw/{Uri.EscapeDataString(secretName)}", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var client = clients.CreateClient("infisical");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Required("INFISICAL_TOKEN"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Workspace-Id", Required("INFISICAL_WORKSPACE_ID"));
        request.Headers.Add("X-Environment", EnvironmentName());
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private string Required(string name) => configuration[name] is { Length: > 0 } value ? value : throw new InvalidOperationException($"{name} is not configured.");
    private string EnvironmentName() => configuration["INFISICAL_ENVIRONMENT"] is { Length: > 0 } value ? value : "production";
    private sealed record SecretResponse(string? SecretValue);
}
