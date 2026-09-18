using AegisCore.Contracts.Abstractions.Cloud;
using Google.Cloud.SecretManager.V1;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisCore.Infrastructure.Cloud.Gcp;

public class GcpSecretManagerService : ISecretService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GcpSecretManagerService> _logger;
    private Dictionary<string, string>? _cache;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public GcpSecretManagerService(IConfiguration configuration, ILogger<GcpSecretManagerService> logger)
    { _configuration = configuration; _logger = logger; }

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken ct)
    {
        if (_cache is not null) return _cache;
        await _gate.WaitAsync(ct);
        try
        {
            if (_cache is not null) return _cache;
            var projectId = _configuration["GCP:ProjectId"] ?? throw new InvalidOperationException("GCP:ProjectId not configured.");
            var secretId = _configuration["GCP:SecretId"] ?? "aegiscore-config";
            var client = SecretManagerServiceClient.Create();
            var response = await client.AccessSecretVersionAsync(
                new SecretVersionName(projectId, secretId, "latest"), ct);
            var data = response.Payload.Data.ToStringUtf8();
            _cache = ParseSecret(data, secretId);
            return _cache;
        }
        finally { _gate.Release(); }
    }

    private static Dictionary<string, string> ParseSecret(string? data, string fallbackKey)
    {
        if (string.IsNullOrWhiteSpace(data)) return new Dictionary<string, string>();
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(data) ?? new Dictionary<string, string>(); }
        catch (JsonException) { return new Dictionary<string, string> { [fallbackKey] = data }; }
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        var dict = await LoadAsync(ct);
        if (dict.TryGetValue(secretName, out var value)) return value;
        throw new KeyNotFoundException($"Secret '{secretName}' not found in GCP Secret Manager.");
    }

    public async Task SetSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        var dict = await LoadAsync(ct);
        dict[secretName] = value;
        var projectId = _configuration["GCP:ProjectId"] ?? throw new InvalidOperationException("GCP:ProjectId not configured.");
        var secretId = _configuration["GCP:SecretId"] ?? "aegiscore-config";
        var client = SecretManagerServiceClient.Create();
        await client.AddSecretVersionAsync(new SecretName(projectId, secretId),
            new SecretPayload { Data = Google.Protobuf.ByteString.CopyFromUtf8(JsonSerializer.Serialize(dict)) });
        _cache = dict;
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken ct = default)
    {
        var dict = await LoadAsync(ct);
        dict.Remove(secretName);
        var projectId = _configuration["GCP:ProjectId"] ?? throw new InvalidOperationException("GCP:ProjectId not configured.");
        var secretId = _configuration["GCP:SecretId"] ?? "aegiscore-config";
        var client = SecretManagerServiceClient.Create();
        await client.AddSecretVersionAsync(new SecretName(projectId, secretId),
            new SecretPayload { Data = Google.Protobuf.ByteString.CopyFromUtf8(JsonSerializer.Serialize(dict)) });
        _cache = dict;
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken ct = default)
    {
        var dict = await LoadAsync(ct);
        return dict.ContainsKey(secretName);
    }
}

public class GmailApiEmailService : IEmailService
{
    private readonly ILogger<GmailApiEmailService> _logger;
    public GmailApiEmailService(IConfiguration configuration, ILogger<GmailApiEmailService> logger) { _logger = logger; }
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Gmail API email to {To}: {Subject}", message.To, message.Subject); }
    public Task SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default) => Task.CompletedTask;
}

public class GoogleWorkspaceCalendarService : ICalendarService
{
    private readonly ILogger<GoogleWorkspaceCalendarService> _logger;
    public GoogleWorkspaceCalendarService(IConfiguration configuration, ILogger<GoogleWorkspaceCalendarService> logger) { _logger = logger; }
    public async Task<string> CreateEventAsync(CalendarEvent e, string orgId, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Google Calendar event: {Title}", e.Title); return Guid.NewGuid().ToString(); }
    public Task UpdateEventAsync(string id, CalendarEvent e, string orgId, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteEventAsync(string id, string orgId, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendInvitesAsync(string id, IEnumerable<string> emails, CancellationToken ct = default) => Task.CompletedTask;
}

public class GoogleWorkspaceIdentityService : IIdentityProviderService
{
    private readonly ILogger<GoogleWorkspaceIdentityService> _logger;
    public GoogleWorkspaceIdentityService(IConfiguration configuration, ILogger<GoogleWorkspaceIdentityService> logger) { _logger = logger; }
    public async Task<ProvisionedUser> ProvisionUserAsync(ProvisionUserCommand cmd, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Google Workspace provision: {Email}", cmd.Email); return new ProvisionedUser(cmd.ExternalId, cmd.Email, true, Guid.NewGuid().ToString()); }
    public async Task DeprovisionUserAsync(string externalId, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Google Workspace deprovision: {Id}", externalId); }
    public Task<string> GetSsoLoginUrlAsync(string tenantId, string returnUrl, CancellationToken ct = default) => Task.FromResult($"https://accounts.google.com/o/oauth2/auth?redirect={returnUrl}");
    public async Task<SsoTokenResult> ValidateSsoCallbackAsync(string code, string state, CancellationToken ct = default) { await Task.CompletedTask; return new SsoTokenResult(true, "user-gcp", "user@gcp.com", "GCP User"); }
    public Task SyncDirectoryAsync(Guid tenantId, CancellationToken ct = default) => Task.CompletedTask;
}
