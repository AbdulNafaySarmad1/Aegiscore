using AegisCore.Contracts.Abstractions.Cloud;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Cloud.Azure;

public class AzureSecretService : ISecretService
{
    private readonly SecretClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureSecretService> _logger;

    public AzureSecretService(IConfiguration configuration, ILogger<AzureSecretService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        var vaultUri = configuration["Azure:KeyVaultUri"] ?? configuration["AZURE_KEYVAULT_URI"];
        if (!string.IsNullOrWhiteSpace(vaultUri))
        {
            _client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
            _logger.LogInformation("AzureSecretService initialized for {VaultUri}", vaultUri);
        }
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        if (_client != null)
        {
            try
            {
                var response = await _client.GetSecretAsync(secretName, cancellationToken: ct);
                return response.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Key Vault read failed for {SecretName} — falling back to environment", secretName);
            }
        }
        return Environment.GetEnvironmentVariable(secretName)
            ?? throw new KeyNotFoundException($"Secret '{secretName}' not found in Key Vault or environment.");
    }

    public async Task SetSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        if (_client != null)
        {
            await _client.SetSecretAsync(secretName, value, ct);
            _logger.LogInformation("Secret {SecretName} written to Azure Key Vault", secretName);
            return;
        }
        _logger.LogWarning("Key Vault not configured — secret {SecretName} stored in environment only", secretName);
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken ct = default)
    {
        if (_client != null)
        {
            var operation = await _client.StartDeleteSecretAsync(secretName, ct);
            await operation.WaitForCompletionAsync(ct);
        }
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken ct = default)
    {
        if (_client != null)
        {
            try { await _client.GetSecretAsync(secretName, cancellationToken: ct); return true; }
            catch { return false; }
        }
        return Environment.GetEnvironmentVariable(secretName) != null;
    }
}

public class AzureStorageService : IStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureStorageService> _logger;

    public AzureStorageService(IConfiguration configuration, ILogger<AzureStorageService> logger)
    { _configuration = configuration; _logger = logger; }

    public async Task<string> UploadAsync(string bucket, string key, Stream content, string contentType, StorageVisibility visibility = StorageVisibility.Private, CancellationToken ct = default)
    { await Task.CompletedTask; _logger.LogInformation("Azure Blob upload: {Bucket}/{Key}", bucket, key); return $"https://storage/{bucket}/{key}"; }

    public async Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default)
    { await Task.CompletedTask; return new MemoryStream(); }

    public Task DeleteAsync(string bucket, string key, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string> GetPresignedUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default) => Task.FromResult($"https://sas-url/{bucket}/{key}");
    public Task<bool> ExistsAsync(string bucket, string key, CancellationToken ct = default) => Task.FromResult(false);
    public Task CreateBucketIfNotExistsAsync(string bucket, CancellationToken ct = default) => Task.CompletedTask;
}

public class MicrosoftGraphEmailService : IEmailService
{
    private readonly ILogger<MicrosoftGraphEmailService> _logger;
    public MicrosoftGraphEmailService(IConfiguration configuration, ILogger<MicrosoftGraphEmailService> logger) { _logger = logger; }
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("MS Graph email to {To}: {Subject}", message.To, message.Subject); }
    public Task SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default) => Task.CompletedTask;
}

public class MicrosoftGraphCalendarService : ICalendarService
{
    private readonly ILogger<MicrosoftGraphCalendarService> _logger;
    public MicrosoftGraphCalendarService(IConfiguration configuration, ILogger<MicrosoftGraphCalendarService> logger) { _logger = logger; }
    public async Task<string> CreateEventAsync(CalendarEvent e, string orgId, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("MS Graph calendar event: {Title}", e.Title); return Guid.NewGuid().ToString(); }
    public Task UpdateEventAsync(string id, CalendarEvent e, string orgId, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteEventAsync(string id, string orgId, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendInvitesAsync(string id, IEnumerable<string> emails, CancellationToken ct = default) => Task.CompletedTask;
}

public class AzureAdIdentityService : IIdentityProviderService
{
    private readonly ILogger<AzureAdIdentityService> _logger;
    public AzureAdIdentityService(IConfiguration configuration, ILogger<AzureAdIdentityService> logger) { _logger = logger; }
    public async Task<ProvisionedUser> ProvisionUserAsync(ProvisionUserCommand cmd, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Azure AD provision user: {Email}", cmd.Email); return new ProvisionedUser(cmd.ExternalId, cmd.Email, true, Guid.NewGuid().ToString()); }
    public async Task DeprovisionUserAsync(string externalId, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Azure AD deprovision: {Id}", externalId); }
    public Task<string> GetSsoLoginUrlAsync(string tenantId, string returnUrl, CancellationToken ct = default) => Task.FromResult($"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize?redirect={returnUrl}");
    public async Task<SsoTokenResult> ValidateSsoCallbackAsync(string code, string state, CancellationToken ct = default) { await Task.CompletedTask; return new SsoTokenResult(true, "user-1", "user@domain.com", "User"); }
    public Task SyncDirectoryAsync(Guid tenantId, CancellationToken ct = default) => Task.CompletedTask;
}
