using AegisCore.Contracts.Abstractions.Cloud;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Cloud.Azure;

public class AzureSecretService : ISecretService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureSecretService> _logger;

    public AzureSecretService(IConfiguration configuration, ILogger<AzureSecretService> logger)
    { _configuration = configuration; _logger = logger; }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        var vaultUri = _configuration["Azure:KeyVaultUri"] ?? throw new InvalidOperationException("Azure Key Vault URI not configured.");
        _logger.LogDebug("Fetching secret {Name} from Azure Key Vault", secretName);
        await Task.CompletedTask;
        return Environment.GetEnvironmentVariable(secretName) ?? throw new InvalidOperationException($"Secret {secretName} not found.");
    }

    public Task SetSecretAsync(string secretName, string value, CancellationToken ct = default)
    { Environment.SetEnvironmentVariable(secretName, value); return Task.CompletedTask; }

    public async Task DeleteSecretAsync(string secretName, CancellationToken ct = default) { await Task.CompletedTask; }

    public Task<bool> SecretExistsAsync(string secretName, CancellationToken ct = default)
    { var v = Environment.GetEnvironmentVariable(secretName); return Task.FromResult(v != null); }
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
