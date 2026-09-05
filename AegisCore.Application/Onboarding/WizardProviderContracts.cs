namespace AegisCore.Application.Onboarding;

public sealed record SsoConfigDto(
    string Provider,
    string? TenantId = null,
    string? ClientId = null,
    string? ClientSecret = null,
    string? Domain = null,
    string? MetadataUrl = null,
    string? MetadataXml = null,
    string? EntityId = null,
    string? AcsUrl = null);

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
    Task ConfigureSsoAsync(string token, SsoConfigDto config, CancellationToken ct = default);
    Task<HandshakeResult> TestSsoAsync(string token, CancellationToken ct = default);
    Task ConfigureAiAsync(string token, AiConfigDto config, CancellationToken ct = default);
    Task<AiConnectionResult> TestAiAsync(string token, CancellationToken ct = default);
}
