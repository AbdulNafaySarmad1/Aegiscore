using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AegisCore.Contracts.Abstractions.Cloud;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Cloud.Infisical;

public sealed class InfisicalSecretService(IHttpClientFactory clients, IConfiguration configuration, IConnectionMultiplexer redis) : ISecretService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int CacheSeconds = 900;

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(secretName);
        var cached = await TryReadCacheAsync(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var location = Location(secretName);
        using var response = await SendAsync(HttpMethod.Get, SecretUri(location), null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<SecretEnvelope>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Secret provider returned an empty response.");
        var value = payload.Secret.SecretValue ?? throw new InvalidOperationException("Secret provider returned no value.");
        await TryWriteCacheAsync(cacheKey, value, cancellationToken);
        return value;
    }

    public async Task SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName) || string.IsNullOrEmpty(value)) throw new ArgumentException("Secret name and value are required.");
        var location = Location(secretName);
        var body = new { projectId = Required("INFISICAL_WORKSPACE_ID"), environment = EnvironmentName(), secretPath = location.Path, secretValue = value, type = "shared" };
        var method = await SecretExistsAsync(secretName, cancellationToken) ? HttpMethod.Patch : HttpMethod.Post;
        using var response = await SendAsync(method, $"/api/v4/secrets/{Uri.EscapeDataString(location.Name)}", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        await InvalidateCacheAsync(secretName, cancellationToken);
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var location = Location(secretName);
        var body = new { projectId = Required("INFISICAL_WORKSPACE_ID"), environment = EnvironmentName(), secretPath = location.Path, type = "shared" };
        using var response = await SendAsync(HttpMethod.Delete, $"/api/v4/secrets/{Uri.EscapeDataString(location.Name)}", body, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
        await InvalidateCacheAsync(secretName, cancellationToken);
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var location = Location(secretName);
        using var response = await SendAsync(HttpMethod.Get, SecretUri(location), null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var client = clients.CreateClient("infisical");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Required("INFISICAL_TOKEN"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private string Required(string name) => configuration[name] is { Length: > 0 } value ? value : throw new InvalidOperationException($"{name} is not configured.");
    private string EnvironmentName() => configuration["INFISICAL_ENVIRONMENT"] is { Length: > 0 } value ? value : "production";
    private string CacheKey(string secretName) => $"infisical:secret:{configuration["INFISICAL_WORKSPACE_ID"]}:{EnvironmentName()}:{secretName}";
    private string SecretUri(SecretLocation location)
        => $"/api/v4/secrets/{Uri.EscapeDataString(location.Name)}?projectId={Uri.EscapeDataString(Required("INFISICAL_WORKSPACE_ID"))}&environment={Uri.EscapeDataString(EnvironmentName())}&secretPath={Uri.EscapeDataString(location.Path)}";

    private static SecretLocation Location(string secretName)
    {
        if (string.IsNullOrWhiteSpace(secretName) || secretName.Length > 512 || secretName.Any(char.IsControl))
            throw new ArgumentException("Secret name is invalid.");
        var normalized = secretName.StartsWith('/') ? secretName : $"/{secretName}";
        var slash = normalized.LastIndexOf('/');
        var name = normalized[(slash + 1)..];
        if (name.Length == 0) throw new ArgumentException("Secret name is invalid.");
        return new(name, slash == 0 ? "/" : normalized[..slash]);
    }

    private async Task<string?> TryReadCacheAsync(string key, CancellationToken ct)
    {
        try { return await redis.GetDatabase().StringGetAsync(key).WaitAsync(ct); }
        catch (RedisException) { return null; }
    }

    private async Task TryWriteCacheAsync(string key, string value, CancellationToken ct)
    {
        try { await redis.GetDatabase().StringSetAsync(key, value, TimeSpan.FromSeconds(CacheSeconds)).WaitAsync(ct); }
        catch (RedisException) { }
    }

    private async Task InvalidateCacheAsync(string secretName, CancellationToken ct)
    {
        try { await redis.GetDatabase().KeyDeleteAsync(CacheKey(secretName)).WaitAsync(ct); }
        catch (RedisException) { }
    }

    private sealed record SecretEnvelope(SecretResponse Secret);
    private sealed record SecretResponse(string? SecretValue);
    private sealed record SecretLocation(string Name, string Path);
}
