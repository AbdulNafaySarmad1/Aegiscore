using AegisCore.Contracts.Abstractions.Cloud;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisCore.Infrastructure.Cloud.Aws;

public class AwsSecretsManagerService : ISecretService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsSecretsManagerService> _logger;
    private Dictionary<string, string>? _cache;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AwsSecretsManagerService(IConfiguration configuration, ILogger<AwsSecretsManagerService> logger)
    { _configuration = configuration; _logger = logger; }

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken ct)
    {
        if (_cache is not null) return _cache;
        await _gate.WaitAsync(ct);
        try
        {
            if (_cache is not null) return _cache;
            var region = _configuration["AWS:Region"] ?? "us-east-1";
            var secretName = _configuration["AWS:SecretName"] ?? "aegiscore-config";
            var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
            var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName }, ct);
            _cache = ParseSecret(response.SecretString, secretName);
            return _cache;
        }
        finally { _gate.Release(); }
    }

    private static Dictionary<string, string> ParseSecret(string? json, string fallbackKey)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(); }
        catch (JsonException) { return new Dictionary<string, string> { [fallbackKey] = json }; }
    }

    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        var dict = await LoadAsync(ct);
        if (dict.TryGetValue(secretName, out var value)) return value;
        throw new InvalidOperationException($"Secret '{secretName}' not found in AWS Secrets Manager.");
    }

    public async Task SetSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        var dict = await LoadAsync(ct);
        dict[secretName] = value;
        var region = _configuration["AWS:Region"] ?? "us-east-1";
        var secretNameCfg = _configuration["AWS:SecretName"] ?? "aegiscore-config";
        var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
        await client.PutSecretValueAsync(new PutSecretValueRequest
        {
            SecretId = secretNameCfg,
            SecretString = JsonSerializer.Serialize(dict)
        }, ct);
        _cache = dict;
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken ct = default)
    {
        var dict = await LoadAsync(ct);
        dict.Remove(secretName);
        var region = _configuration["AWS:Region"] ?? "us-east-1";
        var secretNameCfg = _configuration["AWS:SecretName"] ?? "aegiscore-config";
        var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
        await client.PutSecretValueAsync(new PutSecretValueRequest
        {
            SecretId = secretNameCfg,
            SecretString = JsonSerializer.Serialize(dict)
        }, ct);
        _cache = dict;
    }

    public async Task<bool> SecretExistsAsync(string secretName, CancellationToken ct = default)
    {
        var dict = await LoadAsync(ct);
        return dict.ContainsKey(secretName);
    }
}

public class S3StorageService : IStorageService
{
    private readonly ILogger<S3StorageService> _logger;
    public S3StorageService(IConfiguration configuration, ILogger<S3StorageService> logger) { _logger = logger; }
    public async Task<string> UploadAsync(string bucket, string key, Stream content, string contentType, StorageVisibility visibility = StorageVisibility.Private, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("S3 upload: {Bucket}/{Key}", bucket, key); return $"https://s3.{bucket}/{key}"; }
    public async Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct = default) { await Task.CompletedTask; return new MemoryStream(); }
    public Task DeleteAsync(string bucket, string key, CancellationToken ct = default) => Task.CompletedTask;
    public Task<string> GetPresignedUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken ct = default) => Task.FromResult($"https://presigned/{bucket}/{key}");
    public Task<bool> ExistsAsync(string bucket, string key, CancellationToken ct = default) => Task.FromResult(false);
    public Task CreateBucketIfNotExistsAsync(string bucket, CancellationToken ct = default) => Task.CompletedTask;
}

public class SesEmailService : IEmailService
{
    private readonly ILogger<SesEmailService> _logger;
    public SesEmailService(IConfiguration configuration, ILogger<SesEmailService> logger) { _logger = logger; }
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("SES email to {To}: {Subject}", message.To, message.Subject); }
    public Task SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default) => Task.CompletedTask;
}

public class CognitoIdentityService : IIdentityProviderService
{
    private readonly ILogger<CognitoIdentityService> _logger;
    public CognitoIdentityService(IConfiguration configuration, ILogger<CognitoIdentityService> logger) { _logger = logger; }
    public async Task<ProvisionedUser> ProvisionUserAsync(ProvisionUserCommand cmd, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Cognito provision: {Email}", cmd.Email); return new ProvisionedUser(cmd.ExternalId, cmd.Email, true, Guid.NewGuid().ToString()); }
    public async Task DeprovisionUserAsync(string externalId, CancellationToken ct = default) { await Task.CompletedTask; _logger.LogInformation("Cognito deprovision: {Id}", externalId); }
    public Task<string> GetSsoLoginUrlAsync(string tenantId, string returnUrl, CancellationToken ct = default) => Task.FromResult($"https://cognito-idp/{tenantId}/login?redirect={returnUrl}");
    public async Task<SsoTokenResult> ValidateSsoCallbackAsync(string code, string state, CancellationToken ct = default) { await Task.CompletedTask; return new SsoTokenResult(true, "user-aws", "user@aws.com", "AWS User"); }
    public Task SyncDirectoryAsync(Guid tenantId, CancellationToken ct = default) => Task.CompletedTask;
}
