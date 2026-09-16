using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            throw new KeyNotFoundException($"Secret '{secretName}' not found in environment variables.");
        return Task.FromResult(value);
    }

    public Task SetSecretAsync(string secretName, string value, CancellationToken ct = default)
    { Environment.SetEnvironmentVariable(secretName, value); return Task.CompletedTask; }

    public Task DeleteSecretAsync(string secretName, CancellationToken ct = default) => Task.CompletedTask;

    public Task<bool> SecretExistsAsync(string secretName, CancellationToken ct = default)
    { var v = Environment.GetEnvironmentVariable(secretName); return Task.FromResult(v != null); }
}

public sealed class EncryptedFileSecretService : ISecretService
{
    private readonly string _path;
    private readonly byte[] _key;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public EncryptedFileSecretService(IConfiguration configuration)
    {
        _path = configuration["AEGISCORE_LOCAL_SECRET_PATH"] ?? "/app/config/secrets/vault.bin";
        var encodedKey = configuration["AEGISCORE_PQC_KEY_BASE64"]
            ?? throw new InvalidOperationException("AEGISCORE_PQC_KEY_BASE64 is required for the encrypted local secret vault.");
        _key = Convert.FromBase64String(encodedKey);
        if (_key.Length != 32) throw new InvalidOperationException("AEGISCORE_PQC_KEY_BASE64 must decode to exactly 32 bytes.");
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        ValidateName(secretName);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var values = await ReadAsync(cancellationToken);
            return values.TryGetValue(secretName, out var value)
                ? value
                : throw new KeyNotFoundException("The requested secret is unavailable.");
        }
        finally { _gate.Release(); }
    }

    public async Task SetSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        ValidateName(secretName);
        if (string.IsNullOrEmpty(value) || value.Length > 1_048_576) throw new ArgumentException("Secret value is required and must not exceed 1 MiB.");
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var values = await ReadAsync(cancellationToken);
            values[secretName] = value;
            await WriteAsync(values, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        ValidateName(secretName);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var values = await ReadAsync(cancellationToken);
            if (values.Remove(secretName)) await WriteAsync(values, cancellationToken);
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        ValidateName(secretName);
        await _gate.WaitAsync(cancellationToken);
        try { return (await ReadAsync(cancellationToken)).ContainsKey(secretName); }
        finally { _gate.Release(); }
    }

    private async Task<Dictionary<string, string>> ReadAsync(CancellationToken ct)
    {
        if (!File.Exists(_path)) return new(StringComparer.Ordinal);
        var envelope = await File.ReadAllBytesAsync(_path, ct);
        if (envelope.Length < 29 || envelope[0] != 1) throw new CryptographicException("The local secret vault is invalid.");
        var plaintext = new byte[envelope.Length - 29];
        try
        {
            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(envelope.AsSpan(1, 12), envelope.AsSpan(29), envelope.AsSpan(13, 16), plaintext);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(plaintext)
                ?? new(StringComparer.Ordinal);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private async Task WriteAsync(Dictionary<string, string> values, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_path) ?? throw new InvalidOperationException("Local secret vault path is invalid.");
        Directory.CreateDirectory(directory);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(values);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var envelope = new byte[29 + plaintext.Length];
        envelope[0] = 1;
        nonce.CopyTo(envelope, 1);
        try
        {
            using var aes = new AesGcm(_key, 16);
            aes.Encrypt(nonce, plaintext, envelope.AsSpan(29), envelope.AsSpan(13, 16));
            var temporary = $"{_path}.{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllBytesAsync(temporary, envelope, ct);
                if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                File.Move(temporary, _path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(envelope);
        }
    }

    private static void ValidateName(string secretName)
    {
        if (string.IsNullOrWhiteSpace(secretName) || secretName.Length > 512 || secretName.Any(char.IsControl))
            throw new ArgumentException("Secret name is invalid.");
    }
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
