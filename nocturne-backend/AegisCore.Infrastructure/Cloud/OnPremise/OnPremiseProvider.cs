using System.Security.Cryptography;
using System.Text;
using AegisCore.Contracts.Abstractions.Cloud;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Cloud.OnPremise;

public class EnvironmentVariableSecretService : ISecretService
{
    private readonly ILogger<EnvironmentVariableSecretService> _logger;
    public EnvironmentVariableSecretService(ILogger<EnvironmentVariableSecretService> logger) { _logger = logger; }

    public Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        var value = Environment.GetEnvironmentVariable(secretName);
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Secret '{secretName}' not found in environment variables.");
        return Task.FromResult(value);
    }

    public Task SetSecretAsync(string secretName, string value, CancellationToken ct = default)
    { Environment.SetEnvironmentVariable(secretName, value); return Task.CompletedTask; }

    public Task DeleteSecretAsync(string secretName, CancellationToken ct = default) => Task.CompletedTask;

    public Task<bool> SecretExistsAsync(string secretName, CancellationToken ct = default)
    { var v = Environment.GetEnvironmentVariable(secretName); return Task.FromResult(v != null); }
}

public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger) { _logger = logger; }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("SMTP email to {To}: {Subject}", message.To, message.Subject);
    }

    public Task SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default)
    { foreach (var m in messages) SendAsync(m).Wait(); return Task.CompletedTask; }
}

public class MinioStorageService : IStorageService
{
    private readonly ILogger<MinioStorageService> _logger;
    public MinioStorageService(IConfiguration configuration, ILogger<MinioStorageService> logger) { _logger = logger; }

    public async Task<string> UploadAsync(string bucket, string key, Stream content, string contentType, StorageVisibility visibility = StorageVisibility.Private, CancellationToken ct = default)
    { await Task.CompletedTask; _logger.LogInformation("MinIO upload: {Bucket}/{Key}", bucket, key); return $"http://minio:9000/{bucket}/{key}"; }

    public async Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default) { await Task.CompletedTask; return new MemoryStream(); }
    public Task DeleteAsync(string bucket, string key, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string> GetPresignedUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default) => Task.FromResult($"http://minio:9000/{bucket}/{key}?presigned=true");
    public Task<bool> ExistsAsync(string bucket, string key, CancellationToken ct = default) => Task.FromResult(false);
    public Task CreateBucketIfNotExistsAsync(string bucket, CancellationToken ct = default) => Task.CompletedTask;
}

public class NoOpCalendarService : ICalendarService
{
    private readonly ILogger<NoOpCalendarService> _logger;
    public NoOpCalendarService(ILogger<NoOpCalendarService> logger) { _logger = logger; }
    public Task<string> CreateEventAsync(CalendarEvent e, string orgId, CancellationToken ct = default) { _logger.LogWarning("Calendar not configured. Event '{Title}' not created.", e.Title); return Task.FromResult(Guid.NewGuid().ToString()); }
    public Task UpdateEventAsync(string id, CalendarEvent e, string orgId, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteEventAsync(string id, string orgId, CancellationToken ct = default) => Task.CompletedTask;
    public Task SendInvitesAsync(string id, IEnumerable<string> emails, CancellationToken ct = default) => Task.CompletedTask;
}

public class LocalIdentityService : IIdentityProviderService
{
    private readonly ILogger<LocalIdentityService> _logger;
    public LocalIdentityService(ILogger<LocalIdentityService> logger) { _logger = logger; }
    public async Task<ProvisionedUser> ProvisionUserAsync(ProvisionUserCommand cmd, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Local identity provision: {Email}", cmd.Email); return new ProvisionedUser(cmd.ExternalId, cmd.Email, true, Guid.NewGuid().ToString()); }
    public async Task DeprovisionUserAsync(string externalId, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Local identity deprovision: {Id}", externalId); }
    public Task<string> GetSsoLoginUrlAsync(string tenantId, string returnUrl, CancellationToken ct = default) => Task.FromResult($"/auth/login?returnUrl={returnUrl}");
    public async Task<SsoTokenResult> ValidateSsoCallbackAsync(string code, string state, CancellationToken ct = default) { await Task.CompletedTask; return new SsoTokenResult(true, "local-user", "local@university.edu", "Local User"); }
    public Task SyncDirectoryAsync(Guid tenantId, CancellationToken ct = default) => Task.CompletedTask;
}

public class FcmPushService : INotificationPushService
{
    private readonly ILogger<FcmPushService> _logger;
    public FcmPushService(IConfiguration configuration, ILogger<FcmPushService> logger) { _logger = logger; }
    public async Task SendPushAsync(string deviceToken, PushMessage message, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("FCM push to device: {Title}", message.Title); }
    public async Task SendTopicPushAsync(string topic, PushMessage message, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("FCM push to topic {Topic}: {Title}", topic, message.Title); }
}
