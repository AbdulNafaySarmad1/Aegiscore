using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace AegisCore.Application.Onboarding;

public enum FileStorageProvider { AzureBlob, AwsS3, GcpGcs, MinIO }
public enum SecretsProvider { AzureKeyVault, AwsSecretsManager, GcpSecretManager, EnvironmentVariables }
public enum SmsProvider { Twilio, Vonage, LocalPakistani }

public record WizardError(string UserMessage, string HowToFix, string? FixCommand = null, string? TechnicalDetail = null, WizardError.WizardErrorOwner Owner = WizardError.WizardErrorOwner.UniversityConfig)
{
    public enum WizardErrorOwner { UniversityConfig, NetworkIssue, ContactApexSupport }
}

public record LicenseDto(string Key, string Tier, string IssuedTo, DateTime ExpiresAt, int MaxStudents, int MaxCampuses, string? ActivatedServerFingerprint = null);
public record LicenseValidationResult(bool IsValid, LicenseDto? License = null, WizardError? Error = null);
public record ConnectionTestResult(bool Success, string Message, WizardError? Error = null);
public record DnsPropagationResult(bool IsComplete, int PropagationPercent, string Message);
public record GoLiveResult(bool Success, string? ErpUrl = null, WizardError? Error = null, List<GoLiveCheck>? Checks = null);
public record GoLiveCheck(string Name, bool Passed, string Detail);
public record DomainProvisioningResult(string ErpUrl, DnsRecord? CnameRecord);
public record DnsRecord(string Type, string Name, string Value, string Ttl);

public sealed class WizardStateDto
{
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public int CurrentStep { get; set; }
    public string LicenseMode { get; set; } = "demo";
    public string StepData { get; set; } = "{}";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CreatedByIp { get; set; }

    public LicenseDto? License { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public string UniversityFullName { get; set; } = string.Empty;
    public string UniversityShortName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;

    public string DbDialect { get; set; } = "postgres";
    public string DbHost { get; set; } = string.Empty;
    public int DbPort { get; set; } = 5432;
    public string DbName { get; set; } = string.Empty;
    public string DbUsername { get; set; } = string.Empty;
    public string DbPassword { get; set; } = string.Empty;
    public bool DbSslEnabled { get; set; } = true;

    public string RedisHost { get; set; } = string.Empty;
    public int RedisPort { get; set; } = 6379;
    public string? RedisPassword { get; set; }
    public bool RedisTlsEnabled { get; set; } = true;

    public FileStorageProvider FileStorageProvider { get; set; }
    public string? MinioEndpoint { get; set; }
    public string? MinioAccessKey { get; set; }
    public string? MinioSecretKey { get; set; }
    public string? MinioBucketName { get; set; }

    public SecretsProvider SecretsProvider { get; set; }
    public string? AzureKeyVaultUrl { get; set; }
    public string? AzureClientId { get; set; }
    public string? AzureClientSecret { get; set; }
    public string? AzureTenantId { get; set; }

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SmtpFromAddress { get; set; } = string.Empty;
    public string SmtpFromName { get; set; } = string.Empty;

    public string UniversityDomain { get; set; } = string.Empty;
    public bool UseCloudflareTunnel { get; set; }
    public string? CloudflareTunnelId { get; set; }
    public string? CloudflareTunnelToken { get; set; }
    public string? CloudflareCnameTarget { get; set; }

    public string AdminFullName { get; set; } = string.Empty;
    public string AdminAccountEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;

    [JsonIgnore]
    public string ErpSubdomain => $"erp.{UniversityDomain}";

    [JsonIgnore]
    public string ErpUrl => $"https://erp.{UniversityDomain}";

    [JsonIgnore]
    public string DbConnectionString => DbDialect switch
    {
        "mysql" => $"Server={DbHost};Port={DbPort};Database={DbName};User ID={DbUsername};Password={DbPassword};SslMode={(DbSslEnabled ? "Required" : "Disabled")}",
        "mssql" => $"Server={DbHost},{DbPort};Database={DbName};User ID={DbUsername};Password={DbPassword};Encrypt={DbSslEnabled};TrustServerCertificate={!DbSslEnabled}",
        "oracle" => $"User Id={DbUsername};Password={DbPassword};Data Source={DbHost}:{DbPort}/{DbName}",
        _ => $"Host={DbHost};Port={DbPort};Database={DbName};Username={DbUsername};Password={DbPassword};SslMode={(DbSslEnabled ? "Require" : "Disable")}"
    };

    [JsonIgnore]
    public string RedisConnectionString =>
        $"{RedisHost}:{RedisPort}{(string.IsNullOrEmpty(RedisPassword) ? "" : $",password={RedisPassword}")}{(RedisTlsEnabled ? ",ssl=true" : "")}";
}

public interface IWizardStateStore
{
    Task<WizardStateDto?> LoadAsync(string bootstrapTokenHash, CancellationToken ct = default);
    Task SaveAsync(string bootstrapTokenHash, WizardStateDto state, CancellationToken ct = default);
}

public sealed class WizardSessionNotFoundException()
    : KeyNotFoundException("Setup session not found. Start a new installation.");

public sealed class WizardSessionCompletedException()
    : InvalidOperationException("This installation is already complete.");

public sealed class WizardStateService(IWizardStateStore store)
{
    public async Task<WizardStateDto> GetOrCreateStateAsync(string token, string? createdByIp = null, CancellationToken ct = default)
    {
        var tokenHash = HashToken(token);
        var existing = await store.LoadAsync(tokenHash, ct);
        if (existing is not null) return existing;

        var state = new WizardStateDto
        {
            SessionId = Guid.NewGuid(),
            CreatedByIp = createdByIp,
            StartedAt = DateTimeOffset.UtcNow
        };
        await store.SaveAsync(tokenHash, state, ct);
        return state;
    }

    public async Task<WizardStateDto> LoadState(string token, CancellationToken ct = default)
        => await store.LoadAsync(HashToken(token), ct) ?? throw new WizardSessionNotFoundException();

    public async Task SaveState(string token, WizardStateDto state, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var tokenHash = HashToken(token);
        var existing = await store.LoadAsync(tokenHash, ct);
        if (existing is not null)
        {
            state.SessionId = existing.SessionId;
            if (existing.TenantId != Guid.Empty)
            {
                if (state.TenantId != Guid.Empty && state.TenantId != existing.TenantId)
                    throw new InvalidOperationException("The setup session tenant cannot be changed.");
                state.TenantId = existing.TenantId;
            }
            state.StartedAt = existing.StartedAt;
            state.CreatedByIp ??= existing.CreatedByIp;
        }
        else
        {
            state.SessionId = state.SessionId == Guid.Empty ? Guid.NewGuid() : state.SessionId;
            state.StartedAt = state.StartedAt == default ? DateTimeOffset.UtcNow : state.StartedAt;
        }

        await store.SaveAsync(tokenHash, state, ct);
    }

    public async Task<WizardStateDto> InitializeTenantAtStepOneAsync(string token, CancellationToken ct = default)
    {
        var state = await LoadState(token, ct);
        if (state.CompletedAt is not null) throw new WizardSessionCompletedException();
        if (state.TenantId == Guid.Empty)
        {
            state.TenantId = Guid.NewGuid();
            await SaveState(token, state, ct);
        }
        return state;
    }

    public async Task<WizardStateDto> ResumeWizardAsync(string token, CancellationToken ct = default)
    {
        var state = await LoadState(token, ct);
        if (state.CompletedAt is not null) throw new WizardSessionCompletedException();
        return state;
    }

    public static string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("A setup bootstrap token is required.", nameof(token));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
    }
}
