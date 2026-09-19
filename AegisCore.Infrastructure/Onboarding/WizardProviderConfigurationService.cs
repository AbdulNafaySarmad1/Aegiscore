using System.Net.Http.Headers;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using AegisCore.Application.Onboarding;
using AegisCore.Contracts.Abstractions.Cloud;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardProviderConfigurationService(
    WizardStateService states,
    ISecretService secrets,
    IEnumerable<ISsoProvider> ssoProviders,
    IEnumerable<IAiSetupProvider> aiProviders) : IWizardProviderConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Step 3 holds a list: {"providers":[{provider, ...fields, hasSecret, connectionTested, clientSecretName}]}.
    /// The client secret goes straight to the vault under the tenant's own name — the same name the
    /// provider row will carry after go-live — and never into the wizard payload.
    /// </summary>
    public async Task ConfigureSsoAsync(string token, SsoConfigDto config, CancellationToken ct = default)
    {
        var provider = Find(ssoProviders, config.Provider);
        await provider.ConfigureAsync(config, ct);
        var state = await states.ResumeWizardAsync(token, ct);
        if (state.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before configuring sign-in.");
        var name = Normalize(config.Provider);
        var secretName = SsoSecretName(state.TenantId, name);
        if (!string.IsNullOrWhiteSpace(config.ClientSecret))
            await secrets.SetSecretAsync(secretName, config.ClientSecret, ct);
        var hasSecret = !string.IsNullOrWhiteSpace(config.ClientSecret) || await secrets.SecretExistsAsync(secretName, ct);

        var entry = JsonSerializer.SerializeToNode(config with { Provider = name, ClientSecret = null }, JsonOptions)!.AsObject();
        entry["hasSecret"] = hasSecret;
        entry["clientSecretName"] = hasSecret ? secretName : null;
        // Local needs no handshake; everything else must be tested again after any change.
        entry["connectionTested"] = name == "local";
        var providers = SsoProviders(state);
        var index = IndexOf(providers, item => ProviderName(item) == name);
        if (index >= 0) providers[index] = entry; else providers.Add(entry);
        PutSsoProviders(state, providers);
        await states.SaveState(token, state, ct);
    }

    public async Task<HandshakeResult> TestSsoAsync(string token, string? providerName = null, CancellationToken ct = default)
    {
        var state = await states.ResumeWizardAsync(token, ct);
        var providers = SsoProviders(state);
        var wanted = providerName is null ? null : Normalize(providerName);
        var index = wanted is null
            ? (providers.Count == 1 ? 0 : IndexOf(providers, item => ProviderName(item) != "local"))
            : IndexOf(providers, item => ProviderName(item) == wanted);
        if (index < 0) throw new InvalidOperationException("Configure a sign-in method before testing it.");
        var node = providers[index].AsObject();
        var config = node.Deserialize<SsoConfigDto>(JsonOptions)
            ?? throw new InvalidOperationException("Configure a sign-in method before testing it.");
        var secretName = node["clientSecretName"]?.GetValue<string>() ?? SsoSecretName(state.TenantId, config.Provider);
        if (await secrets.SecretExistsAsync(secretName, ct))
            config = config with { ClientSecret = await secrets.GetSecretAsync(secretName, ct) };
        var provider = Find(ssoProviders, config.Provider);
        await provider.ConfigureAsync(config, ct);
        var result = await provider.TestHandshakeAsync(ct);
        node["connectionTested"] = result.Success;
        node["clientSecret"] = null;
        providers[index] = node;
        PutSsoProviders(state, providers);
        await states.SaveState(token, state, ct);
        return result;
    }

    public async Task RemoveSsoAsync(string token, string providerName, CancellationToken ct = default)
    {
        var state = await states.ResumeWizardAsync(token, ct);
        var name = Normalize(providerName);
        var providers = SsoProviders(state);
        var index = IndexOf(providers, item => ProviderName(item) == name);
        if (index < 0) return;
        var secretName = providers[index].AsObject()["clientSecretName"]?.GetValue<string>();
        providers.RemoveAt(index);
        PutSsoProviders(state, providers);
        await states.SaveState(token, state, ct);
        if (!string.IsNullOrWhiteSpace(secretName))
        {
            try { await secrets.DeleteSecretAsync(secretName, ct); }
            catch (KeyNotFoundException) { /* already gone */ }
        }
    }

    public static string SsoSecretName(Guid tenantId, string provider)
        => $"/tenants/{tenantId:D}/identity/{Normalize(provider)}/client-secret";

    private static string Normalize(string provider) => provider.Trim().ToLowerInvariant();

    private static string ProviderName(JsonNode? node)
        => node?["provider"]?.GetValue<string>() is { } name ? Normalize(name) : string.Empty;

    /// <summary>
    /// The step-3 list. A session written before step 3 held a list carries one object with a
    /// "provider" key; it is read as a list of one so nothing in flight is lost.
    /// </summary>
    public static List<JsonNode> SsoProviders(WizardStateDto state)
    {
        var root = JsonNode.Parse(state.StepData)?.AsObject() ?? new JsonObject();
        var step = root["step3"]?.AsObject();
        if (step is null) return [];
        if (step["providers"] is JsonArray array)
            return array.Where(item => item is JsonObject).Select(item => item!.DeepClone()).ToList();
        return step["provider"] is not null ? [step.DeepClone()] : [];
    }

    private static void PutSsoProviders(WizardStateDto state, List<JsonNode> providers)
    {
        var root = JsonNode.Parse(state.StepData)?.AsObject() ?? new JsonObject();
        var step = new JsonObject { ["providers"] = new JsonArray(providers.Select(item => item.DeepClone()).ToArray()) };
        root["step3"] = step;
        state.StepData = root.ToJsonString(JsonOptions);
    }

    public async Task ConfigureAiAsync(string token, AiConfigDto config, CancellationToken ct = default)
    {
        var provider = Find(aiProviders, config.Provider);
        await provider.ConfigureAsync(config, ct);
        var state = await states.ResumeWizardAsync(token, ct);
        var secretName = SecretName(token, "ai-api-key");
        if (!string.IsNullOrWhiteSpace(config.ApiKey))
            await secrets.SetSecretAsync(secretName, config.ApiKey, ct);

        SetStep(state, 5, JsonSerializer.SerializeToNode(config with { ApiKey = null }, JsonOptions)!.AsObject(),
            hasSecret: !string.IsNullOrWhiteSpace(config.ApiKey) || await secrets.SecretExistsAsync(secretName, ct));
        await states.SaveState(token, state, ct);
    }

    public async Task<AiConnectionResult> TestAiAsync(string token, CancellationToken ct = default)
    {
        var state = await states.ResumeWizardAsync(token, ct);
        var node = GetStep(state, 5);
        var config = node.Deserialize<AiConfigDto>(JsonOptions)
            ?? throw new InvalidOperationException("Configure an AI provider before testing it.");
        var secretName = SecretName(token, "ai-api-key");
        if (await secrets.SecretExistsAsync(secretName, ct))
            config = config with { ApiKey = await secrets.GetSecretAsync(secretName, ct) };
        var provider = Find(aiProviders, config.Provider);
        await provider.ConfigureAsync(config, ct);
        var result = await provider.TestConnectionAsync(ct);
        node["connectionTested"] = result.Success;
        node["apiKey"] = null;
        PutStep(state, 5, node);
        await states.SaveState(token, state, ct);
        return result;
    }

    private static int IndexOf(List<JsonNode> items, Func<JsonNode, bool> predicate)
    {
        for (var i = 0; i < items.Count; i++) if (predicate(items[i])) return i;
        return -1;
    }

    private static T Find<T>(IEnumerable<T> providers, string name) where T : class
        => providers.SingleOrDefault(item => string.Equals(
               item switch { ISsoProvider sso => sso.Name, IAiSetupProvider ai => ai.Name, _ => null },
               name, StringComparison.OrdinalIgnoreCase))
           ?? throw new ArgumentException($"Unsupported provider '{name}'.");

    private static JsonObject GetStep(WizardStateDto state, int step)
    {
        var root = JsonNode.Parse(state.StepData)?.AsObject() ?? new JsonObject();
        state.StepData = root.ToJsonString(JsonOptions);
        return root[$"step{step}"]?.AsObject()
            ?? throw new InvalidOperationException($"Wizard step {step} has not been configured.");
    }

    private static void SetStep(WizardStateDto state, int step, JsonObject value, bool hasSecret)
    {
        var root = JsonNode.Parse(state.StepData)?.AsObject() ?? new JsonObject();
        value["hasSecret"] = hasSecret;
        value["connectionTested"] = false;
        root[$"step{step}"] = value;
        state.StepData = root.ToJsonString(JsonOptions);
    }

    private static void PutStep(WizardStateDto state, int step, JsonObject value)
    {
        var root = JsonNode.Parse(state.StepData)?.AsObject() ?? new JsonObject();
        root[$"step{step}"] = value.DeepClone();
        state.StepData = root.ToJsonString(JsonOptions);
    }

    private static string SecretName(string token, string suffix)
        => $"/wizard/{WizardStateService.HashToken(token).ToLowerInvariant()}/{suffix}";
}

public abstract class SsoProviderBase(string name) : ISsoProvider
{
    protected SsoConfigDto? Config { get; private set; }
    public string Name { get; } = name;
    public virtual Task ConfigureAsync(SsoConfigDto config, CancellationToken ct = default)
    {
        Config = config;
        return Task.CompletedTask;
    }
    public abstract Task<HandshakeResult> TestHandshakeAsync(CancellationToken ct = default);
    protected SsoConfigDto RequiredConfig() => Config ?? throw new InvalidOperationException("Provider is not configured.");
}

public sealed class LocalSsoProvider() : SsoProviderBase("local")
{
    public override Task<HandshakeResult> TestHandshakeAsync(CancellationToken ct = default)
        => Task.FromResult(new HandshakeResult(true));
}

/// <summary>
/// The OpenID Connect providers share one handshake: the discovery document must be fetchable and
/// complete, through the same client that will validate ID tokens at sign-in — not a bare HTTP GET
/// that would pass a URL the login path then chokes on. The login URL returned is the real
/// authorization endpoint, so the administrator can see where people will be sent.
/// </summary>
public abstract class OidcSsoProvider(string name, AegisCore.Infrastructure.Identity.Oidc.IOidcClient oidc, IConfiguration configuration) : SsoProviderBase(name)
{
    protected abstract string? Authority(SsoConfigDto config);
    protected abstract HandshakeResult? MissingFields(SsoConfigDto config);
    protected virtual string FixHint => "Verify DNS, outbound HTTPS access and the values copied from the provider.";

    public override async Task<HandshakeResult> TestHandshakeAsync(CancellationToken ct = default)
    {
        var config = RequiredConfig();
        if (MissingFields(config) is { } missing) return missing;
        if (string.IsNullOrWhiteSpace(config.ClientSecret))
            return new(false, "Client secret is required.", "Create a client secret at the provider and paste its value (not its id).");
        var authority = Authority(config);
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return new(false, "The authority must be an HTTPS URL.", FixHint);
        try
        {
            var discovery = await oidc.DiscoverAsync(authority!, ct);
            if (string.IsNullOrWhiteSpace(discovery.AuthorizationEndpoint) || string.IsNullOrWhiteSpace(discovery.TokenEndpoint) || string.IsNullOrWhiteSpace(discovery.JwksUri))
                return new(false, "The provider's discovery document is incomplete (no authorization, token or JWKS endpoint).", FixHint);
            if (discovery.SigningKeys.Count == 0)
                return new(false, "The provider published no signing keys; ID tokens could not be verified.", FixHint);
            var redirect = AegisCore.Infrastructure.Identity.ExternalSignInService.RedirectUri(configuration);
            return new(true, LoginUrl: discovery.AuthorizationEndpoint, FixHint: $"Register {redirect} as the redirect URI at the provider.");
        }
        catch (AegisCore.Application.Identity.ExternalSignInException ex)
        {
            return new(false, ex.Message, FixHint);
        }
    }
}

public sealed class EntraSsoProvider(AegisCore.Infrastructure.Identity.Oidc.IOidcClient oidc, IConfiguration configuration) : OidcSsoProvider("entra", oidc, configuration)
{
    public static string AuthorityFor(string tenantId) => $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenantId.Trim())}/v2.0";
    protected override string? Authority(SsoConfigDto config) => string.IsNullOrWhiteSpace(config.TenantId) ? null : AuthorityFor(config.TenantId);
    protected override string FixHint => "Verify the directory (tenant) ID, the application (client) ID and the client secret in the Microsoft Entra app registration.";
    protected override HandshakeResult? MissingFields(SsoConfigDto config)
        => string.IsNullOrWhiteSpace(config.TenantId) || string.IsNullOrWhiteSpace(config.ClientId)
            ? new(false, "Directory (tenant) ID and application (client) ID are required.", "Copy both from the app registration's Overview page in Microsoft Entra.")
            : null;
}

public sealed class GoogleWorkspaceSsoProvider(AegisCore.Infrastructure.Identity.Oidc.IOidcClient oidc, IConfiguration configuration) : OidcSsoProvider("google", oidc, configuration)
{
    public const string GoogleAuthority = "https://accounts.google.com";
    protected override string? Authority(SsoConfigDto config) => GoogleAuthority;
    protected override string FixHint => "Verify outbound HTTPS access and the OAuth client in Google Cloud Console.";
    protected override HandshakeResult? MissingFields(SsoConfigDto config)
        => string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.Domain)
            ? new(false, "Client ID and Workspace domain are required.", "Create an OAuth 2.0 web client in Google Cloud Console and enter your Workspace domain (e.g. university.edu.pk).")
            : null;
}

public sealed class OktaSsoProvider(AegisCore.Infrastructure.Identity.Oidc.IOidcClient oidc, IConfiguration configuration) : OidcSsoProvider("okta", oidc, configuration)
{
    protected override string? Authority(SsoConfigDto config) => config.Authority?.Trim().TrimEnd('/');
    protected override string FixHint => "Verify the Okta org URL (https://{org}.okta.com, or https://{org}.okta.com/oauth2/default), the client ID and the client secret of the OIDC web application.";
    protected override HandshakeResult? MissingFields(SsoConfigDto config)
        => string.IsNullOrWhiteSpace(config.Authority) || string.IsNullOrWhiteSpace(config.ClientId)
            ? new(false, "Okta org URL (authority) and client ID are required.", "Create an OIDC Web Application in Okta and copy its client ID; the org URL is at the top right of the Okta admin console.")
            : null;
}

public sealed class GenericSamlSsoProvider(IHttpClientFactory clients) : SsoProviderBase("saml")
{
    public override async Task<HandshakeResult> TestHandshakeAsync(CancellationToken ct = default)
    {
        var config = RequiredConfig();
        if (string.IsNullOrWhiteSpace(config.EntityId) || string.IsNullOrWhiteSpace(config.AcsUrl))
            return new(false, "Entity ID and ACS URL are required.", "Use the service-provider values registered with your identity provider.");
        try
        {
            var metadata = config.MetadataXml;
            if (string.IsNullOrWhiteSpace(metadata))
            {
                if (!TryHttpsUri(config.MetadataUrl, out var uri))
                    return new(false, "Provide metadata XML or an HTTPS metadata URL.", "Export IdP metadata and paste it, or provide its HTTPS URL.");
                metadata = await clients.CreateClient("wizard-providers").GetStringAsync(uri, ct);
            }
            if (metadata.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || metadata.Contains("<!ENTITY", StringComparison.OrdinalIgnoreCase))
                return new(false, "Unsafe SAML metadata was rejected.", "Export metadata without DTD or external entity declarations.");
            var document = new XmlDocument { XmlResolver = null };
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(metadata), settings);
            document.Load(reader);
            var manager = new XmlNamespaceManager(document.NameTable);
            manager.AddNamespace("md", "urn:oasis:names:tc:SAML:2.0:metadata");
            var entityId = document.SelectSingleNode("//md:EntityDescriptor/@entityID", manager)?.Value;
            var login = document.SelectSingleNode("//md:SingleSignOnService/@Location", manager)?.Value;
            return !string.IsNullOrWhiteSpace(entityId) && Uri.TryCreate(login, UriKind.Absolute, out _)
                ? new(true, LoginUrl: BuildSpInitiatedUrl(login!, config.EntityId, config.AcsUrl))
                : new(false, "SAML metadata has no valid entity ID or SSO service.", "Download fresh SAML 2.0 IdP metadata.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or XmlException)
        {
            return new(false, ex.Message, "Verify the metadata URL or XML document.");
        }
    }

    private static bool TryHttpsUri(string? value, out Uri uri)
    {
        var ok = Uri.TryCreate(value, UriKind.Absolute, out var parsed) && parsed.Scheme == Uri.UriSchemeHttps;
        uri = parsed!;
        return ok;
    }

    private static string BuildSpInitiatedUrl(string loginUrl, string entityId, string acsUrl)
    {
        var requestId = "_" + Guid.NewGuid().ToString("N");
        var xml = $"""
            <samlp:AuthnRequest xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol" xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion" ID="{requestId}" Version="2.0" IssueInstant="{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}" Destination="{System.Security.SecurityElement.Escape(loginUrl)}" AssertionConsumerServiceURL="{System.Security.SecurityElement.Escape(acsUrl)}" ProtocolBinding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST"><saml:Issuer>{System.Security.SecurityElement.Escape(entityId)}</saml:Issuer><samlp:NameIDPolicy Format="urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress" AllowCreate="true" /></samlp:AuthnRequest>
            """;
        var input = Encoding.UTF8.GetBytes(xml);
        using var output = new MemoryStream();
        using (var compressor = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
            compressor.Write(input);
        var separator = loginUrl.Contains('?') ? '&' : '?';
        return loginUrl + separator + "SAMLRequest=" + Uri.EscapeDataString(Convert.ToBase64String(output.ToArray()));
    }
}

public abstract class HttpAiSetupProvider(string name, IHttpClientFactory clients) : IAiSetupProvider
{
    protected AiConfigDto? Config { get; private set; }
    protected HttpClient Client => clients.CreateClient("wizard-providers");
    public string Name { get; } = name;
    public virtual Task ConfigureAsync(AiConfigDto config, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(config.Model)) throw new ArgumentException("Model name is required.");
        Config = config;
        return Task.CompletedTask;
    }
    public abstract Task<AiConnectionResult> TestConnectionAsync(CancellationToken ct = default);
    protected AiConfigDto RequiredConfig() => Config ?? throw new InvalidOperationException("Provider is not configured.");

    protected async Task<AiConnectionResult> SendAsync(Uri endpoint, object body, string? apiKey, Func<JsonElement, string?> responseReader, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(apiKey)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var response = await Client.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new(false, Error: $"Provider returned HTTP {(int)response.StatusCode}.", FixHint: "Verify the endpoint, model name and API credentials.");
            using var json = JsonDocument.Parse(content);
            var value = responseReader(json.RootElement);
            return string.IsNullOrWhiteSpace(value)
                ? new(false, Error: "The provider returned no model response.", FixHint: "Verify that the selected model is installed and available.")
                : new(true, Response: value);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(false, Error: ex.Message, FixHint: "Verify network access, endpoint URL, model name and credentials.");
        }
    }

    protected static Uri RequireEndpoint(string? value, string fallback, bool allowLoopback = false)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? fallback : value;
        if (!Uri.TryCreate(candidate.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && !(allowLoopback && uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp)))
            throw new ArgumentException("Provider endpoint must use HTTPS (HTTP is allowed only for loopback Ollama).");
        return uri;
    }
}

public sealed class OllamaLocalSetupProvider(IHttpClientFactory clients) : HttpAiSetupProvider("ollama-local", clients)
{
    public override Task<AiConnectionResult> TestConnectionAsync(CancellationToken ct = default)
    {
        var config = RequiredConfig();
        var endpoint = new Uri(RequireEndpoint(config.Endpoint, "http://localhost:11434", true), "api/generate");
        return SendAsync(endpoint, new { model = config.Model, prompt = "Hello", stream = false }, null,
            root => root.TryGetProperty("response", out var value) ? value.GetString() : null, ct);
    }
}

public sealed class OllamaCloudSetupProvider(IHttpClientFactory clients) : HttpAiSetupProvider("ollama-cloud", clients)
{
    public override Task<AiConnectionResult> TestConnectionAsync(CancellationToken ct = default)
    {
        var config = RequiredConfig();
        var endpoint = new Uri(RequireEndpoint(config.Endpoint, "https://ollama.com"), "api/generate");
        return SendAsync(endpoint, new { model = config.Model, prompt = "Hello", stream = false }, config.ApiKey,
            root => root.TryGetProperty("response", out var value) ? value.GetString() : null, ct);
    }
}

public class OpenAiCompatibleSetupProvider(string name, IHttpClientFactory clients) : HttpAiSetupProvider(name, clients)
{
    protected virtual string DefaultEndpoint => "https://api.openai.com";
    protected virtual Uri CompletionEndpoint(AiConfigDto config) => new(RequireEndpoint(config.Endpoint, DefaultEndpoint), "v1/chat/completions");

    public override Task<AiConnectionResult> TestConnectionAsync(CancellationToken ct = default)
    {
        var config = RequiredConfig();
        if (Name != "custom" && string.IsNullOrWhiteSpace(config.ApiKey))
            return Task.FromResult(new AiConnectionResult(false, Error: "API key is required.", FixHint: "Enter a valid provider API key."));
        return SendAsync(CompletionEndpoint(config), new { model = config.Model, messages = new[] { new { role = "user", content = "Hello" } }, max_tokens = 8 }, config.ApiKey,
            root => root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0 && choices[0].TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content) ? content.GetString() : null, ct);
    }
}

public sealed class OpenAiSetupProvider(IHttpClientFactory clients) : OpenAiCompatibleSetupProvider("openai", clients);
public sealed class CustomAiSetupProvider(IHttpClientFactory clients) : OpenAiCompatibleSetupProvider("custom", clients);

public sealed class GraniteProvider(IHttpClientFactory clients) : HttpAiSetupProvider("granite", clients)
{
    private const string GraniteEndpoint = "https://us-south.ml.cloud.ibm.com/ml/v1/text/generation?version=2023-05-29";
    public override Task<AiConnectionResult> TestConnectionAsync(CancellationToken ct = default)
    {
        var config = RequiredConfig();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            return Task.FromResult(new AiConnectionResult(false, Error: "IBM IAM API key is required.", FixHint: "Create or copy an IAM API key from IBM Cloud."));
        return SendAsync(new Uri(GraniteEndpoint), new { model_id = config.Model, input = "Hello", parameters = new { max_new_tokens = 8 } }, config.ApiKey,
            root => root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0 && results[0].TryGetProperty("generated_text", out var content) ? content.GetString() : null, ct);
    }
}
