namespace AegisCore.Application.Onboarding;

/// <summary>
/// One way of signing in, as the wizard collects it. <c>Provider</c> is "local", "entra", "google",
/// "okta" or "saml". Entra wants <c>TenantId</c>; Google wants <c>Domain</c>; Okta wants
/// <c>Authority</c> (the org URL, or its /oauth2/default authorization server). <c>GroupRoleMapping</c>
/// maps a provider group (id or name) to a role named in the permission step; <c>DefaultRole</c> is
/// for people in no mapped group.
/// </summary>
public sealed record SsoConfigDto(
    string Provider,
    string? TenantId = null,
    string? ClientId = null,
    string? ClientSecret = null,
    string? Domain = null,
    string? MetadataUrl = null,
    string? MetadataXml = null,
    string? EntityId = null,
    string? AcsUrl = null,
    string? Authority = null,
    string? Scopes = null,
    Dictionary<string, string>? GroupRoleMapping = null,
    string? DefaultRole = null);

public sealed record HandshakeResult(bool Success, string? Error = null, string? FixHint = null, string? LoginUrl = null);

public interface ISsoProvider
{
    string Name { get; }
    Task ConfigureAsync(SsoConfigDto config, CancellationToken ct = default);
    Task<HandshakeResult> TestHandshakeAsync(CancellationToken ct = default);
}

public sealed record AiConfigDto(
    string Provider,
    string Model,
    string? Endpoint = null,
    string? ApiKey = null);

public sealed record AiConnectionResult(bool Success, string? Response = null, string? Error = null, string? FixHint = null);

public interface IAiSetupProvider
{
    string Name { get; }
    Task ConfigureAsync(AiConfigDto config, CancellationToken ct = default);
    Task<AiConnectionResult> TestConnectionAsync(CancellationToken ct = default);
}

public interface IWizardProviderConfigurationService
{
    /// <summary>Adds or replaces one sign-in method in step 3; a tenant may enable several.</summary>
    Task ConfigureSsoAsync(string token, SsoConfigDto config, CancellationToken ct = default);
    /// <summary>Tests one configured method (by provider name; null tests the only one / the first).</summary>
    Task<HandshakeResult> TestSsoAsync(string token, string? provider = null, CancellationToken ct = default);
    /// <summary>Removes one sign-in method from step 3, and its secret from the vault.</summary>
    Task RemoveSsoAsync(string token, string provider, CancellationToken ct = default);
    Task ConfigureAiAsync(string token, AiConfigDto config, CancellationToken ct = default);
    Task<AiConnectionResult> TestAiAsync(string token, CancellationToken ct = default);
}
