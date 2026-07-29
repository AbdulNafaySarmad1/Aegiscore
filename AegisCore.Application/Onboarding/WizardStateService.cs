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
public record DomainProvisioningResult(string ErpUrl, DnsRecord CnameRecord);
public record DnsRecord(string Type, string Name, string Value, string Ttl);

public class WizardStateService
{
    public Guid TenantId { get; set; } = Guid.NewGuid();
    public LicenseDto? License { get; set; }

    public string LicenseKey { get; set; } = string.Empty;
    public string UniversityFullName { get; set; } = string.Empty;
    public string UniversityShortName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;

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
    public string? CloudflareTunnelId { get; set; }
    public string? CloudflareTunnelToken { get; set; }
    public string? CloudflareCnameTarget { get; set; }
    public string ErpSubdomain => $"erp.{UniversityDomain}";
    public string ErpUrl => $"https://erp.{UniversityDomain}";

    public string AdminFullName { get; set; } = string.Empty;
    public string AdminAccountEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;

    public string DbConnectionString =>
        $"Host={DbHost};Port={DbPort};Database={DbName};Username={DbUsername};Password={DbPassword};SslMode={(DbSslEnabled ? "Require" : "Disable")}";

    public string RedisConnectionString =>
        $"{RedisHost}:{RedisPort}{(string.IsNullOrEmpty(RedisPassword) ? "" : $",password={RedisPassword}")}{(RedisTlsEnabled ? ",ssl=true" : "")}";
}
