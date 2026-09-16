using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Deployment;

public enum EnvironmentValueType
{
    String,
    Integer
}

public sealed record EnvironmentVariableDefinition(
    string Name,
    string Category,
    EnvironmentValueType Type,
    bool IsRequired,
    string? ConditionalOn = null,
    IReadOnlySet<string>? AllowedValues = null);

public sealed record ValidationResult(
    bool IsValid,
    IReadOnlyList<string> MissingRequired,
    IReadOnlyList<string> MissingConditional,
    IReadOnlyList<string> Errors);

public interface IEnvironmentValidator
{
    ValidationResult Validate();
    IReadOnlyList<string> GetMissingRequired();
    IReadOnlyList<string> GetMissingConditional(string condition);
}

public sealed class EnvironmentValidator(IConfiguration configuration) : IEnvironmentValidator
{
    public const string SsoNonLocalCondition = "SSO_PROVIDER != local";
    public const string SsoEntraCondition = "SSO_PROVIDER = entra";
    public const string AiRemoteCondition = "AI_PROVIDER != disabled";
    /// <summary>DB_HOST..DB_PASS describe the database only when no explicit connection string (AEGISCORE_DB_CONNECTION) is given.</summary>
    public const string NoExplicitConnectionCondition = "AEGISCORE_DB_CONNECTION unset";

    public static IReadOnlyList<EnvironmentVariableDefinition> Registry { get; } =
    [
        new("DB_DIALECT", "Database", EnvironmentValueType.String, false, AllowedValues: Values("postgres", "mysql", "mssql", "oracle")),
        new("DB_HOST", "Database", EnvironmentValueType.String, false, NoExplicitConnectionCondition),
        new("DB_PORT", "Database", EnvironmentValueType.Integer, false, NoExplicitConnectionCondition),
        new("DB_NAME", "Database", EnvironmentValueType.String, false, NoExplicitConnectionCondition),
        new("DB_USER", "Database", EnvironmentValueType.String, false, NoExplicitConnectionCondition),
        new("DB_PASS", "Database", EnvironmentValueType.String, false, NoExplicitConnectionCondition),

        new("SSO_PROVIDER", "Auth / SSO", EnvironmentValueType.String, false, AllowedValues: Values("okta", "entra", "google", "saml", "local")),
        new("SSO_CLIENT_ID", "Auth / SSO", EnvironmentValueType.String, false, SsoNonLocalCondition),
        new("SSO_CLIENT_SECRET", "Auth / SSO", EnvironmentValueType.String, false, SsoNonLocalCondition),
        new("SSO_TENANT_ID", "Auth / SSO", EnvironmentValueType.String, false, SsoEntraCondition),
        new("SSO_CALLBACK_URL", "Auth / SSO", EnvironmentValueType.String, false, SsoNonLocalCondition),

        new("AI_PROVIDER", "AI / Helpbot", EnvironmentValueType.String, true, AllowedValues: Values("disabled", "ollama", "openai", "granite", "custom")),
        new("AI_ENDPOINT", "AI / Helpbot", EnvironmentValueType.String, false, AiRemoteCondition),
        new("AI_MODEL", "AI / Helpbot", EnvironmentValueType.String, false, AiRemoteCondition),
        new("AI_API_KEY", "AI / Helpbot", EnvironmentValueType.String, false, AiRemoteCondition),

        new("PQC_MLKEM_PUBLIC_KEY", "Security / PQC", EnvironmentValueType.String, true),
        new("PQC_MLKEM_PRIVATE_KEY", "Security / PQC", EnvironmentValueType.String, true),
        new("PQC_MLDSA_PUBLIC_KEY", "Security / PQC", EnvironmentValueType.String, true),
        new("PQC_MLDSA_PRIVATE_KEY", "Security / PQC", EnvironmentValueType.String, true),

        // Turnstile is verified only when a token and a secret are both present; an on-premises
        // deployment behind the university ingress runs without Cloudflare.
        new("CF_TURNSTILE_SITE_KEY", "Cloudflare", EnvironmentValueType.String, false),
        new("CF_TURNSTILE_SECRET_KEY", "Cloudflare", EnvironmentValueType.String, false),

        new("APP_URL", "App", EnvironmentValueType.String, true),
        new("APP_ENV", "App", EnvironmentValueType.String, true, AllowedValues: Values("Production", "Staging", "Development")),
        new("APP_SECRET", "App", EnvironmentValueType.String, true),
        new("SESSION_LIFETIME", "App", EnvironmentValueType.Integer, true),
        new("AEGISCORE_SETUP_TOKEN", "App", EnvironmentValueType.String, true),
        new("AEGISCORE_ENCRYPTION_KEY", "App", EnvironmentValueType.String, true)
    ];

    public ValidationResult Validate()
    {
        var missingRequired = GetMissingRequired();
        var missingConditional = Registry
            .Where(definition => definition.ConditionalOn is not null && IsConditionActive(definition.ConditionalOn))
            .Where(definition => IsMissing(Value(definition.Name)))
            .Select(definition => definition.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var errors = Registry
            .Select(ValidateValue)
            .Where(error => error is not null)
            .Cast<string>()
            .ToArray();

        return new ValidationResult(
            missingRequired.Count == 0 && missingConditional.Length == 0 && errors.Length == 0,
            missingRequired,
            missingConditional,
            errors);
    }

    public IReadOnlyList<string> GetMissingRequired()
        => Registry
            .Where(definition => definition.IsRequired && IsMissing(Value(definition.Name)))
            .Select(definition => definition.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

    public IReadOnlyList<string> GetMissingConditional(string condition)
        => Registry
            .Where(definition => string.Equals(definition.ConditionalOn, condition, StringComparison.Ordinal))
            .Where(definition => IsConditionActive(condition) && IsMissing(Value(definition.Name)))
            .Select(definition => definition.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private string? ValidateValue(EnvironmentVariableDefinition definition)
    {
        var value = Value(definition.Name);
        if (IsMissing(value)) return null;

        if (definition.Type == EnvironmentValueType.Integer
            && (!int.TryParse(value, out var number) || number <= 0))
            return $"{definition.Name} must be a positive integer.";

        if (definition.AllowedValues is not null && !definition.AllowedValues.Contains(value!))
            return $"{definition.Name} must be one of: {string.Join(", ", definition.AllowedValues.Order(StringComparer.OrdinalIgnoreCase))}.";

        if (definition.Name.EndsWith("_URL", StringComparison.Ordinal)
            || definition.Name.EndsWith("_ENDPOINT", StringComparison.Ordinal))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
                return $"{definition.Name} must be an absolute HTTP or HTTPS URL.";
        }

        return null;
    }

    private bool IsConditionActive(string condition)
    {
        var ssoProvider = Value("SSO_PROVIDER");
        var aiProvider = Value("AI_PROVIDER");
        return condition switch
        {
            SsoNonLocalCondition => !IsMissing(ssoProvider) && !string.Equals(ssoProvider, "local", StringComparison.OrdinalIgnoreCase),
            SsoEntraCondition => string.Equals(ssoProvider, "entra", StringComparison.OrdinalIgnoreCase),
            AiRemoteCondition => !IsMissing(aiProvider)
                && !string.Equals(aiProvider, "disabled", StringComparison.OrdinalIgnoreCase),
            NoExplicitConnectionCondition => IsMissing(Value("AEGISCORE_DB_CONNECTION")) && IsMissing(Value("ConnectionStrings:Default"))
                && IsMissing(Value("Database:ConnectionString")) && IsMissing(Value("POSTGRES_CONNECTION_STRING")),
            _ => false
        };
    }

    private string? Value(string name) => configuration[name]?.Trim();

    private static bool IsMissing(string? value)
        => string.IsNullOrWhiteSpace(value)
            || (value.StartsWith("${", StringComparison.Ordinal) && value.EndsWith('}'))
            || value.StartsWith("replace-with-", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlySet<string> Values(params string[] values)
        => new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
