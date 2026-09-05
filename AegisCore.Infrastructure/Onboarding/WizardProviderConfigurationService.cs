using System.Net.Http.Headers;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using AegisCore.Application.Onboarding;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure.Cloud.Azure;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardProviderConfigurationService(
    WizardStateService states,
    ISecretService secrets,
    IEnumerable<ISsoProvider> ssoProviders,
    IEnumerable<IAiSetupProvider> aiProviders) : IWizardProviderConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task ConfigureSsoAsync(string token, SsoConfigDto config, CancellationToken ct = default)
    {
        var provider = Find(ssoProviders, config.Provider);
        await provider.ConfigureAsync(config, ct);
        var state = await states.ResumeWizardAsync(token, ct);
        var secretName = SecretName(token, "sso-client-secret");
        if (!string.IsNullOrWhiteSpace(config.ClientSecret))
            await secrets.SetSecretAsync(secretName, config.ClientSecret, ct);

        SetStep(state, 3, JsonSerializer.SerializeToNode(config with { ClientSecret = null }, JsonOptions)!.AsObject(),
            hasSecret: !string.IsNullOrWhiteSpace(config.ClientSecret) || await secrets.SecretExistsAsync(secretName, ct));
        await states.SaveState(token, state, ct);
    }

    public async Task<HandshakeResult> TestSsoAsync(string token, CancellationToken ct = default)
    {
        var state = await states.ResumeWizardAsync(token, ct);
        var node = GetStep(state, 3);
        var config = node.Deserialize<SsoConfigDto>(JsonOptions)
            ?? throw new InvalidOperationException("Configure SSO before testing the handshake.");
        var secretName = SecretName(token, "sso-client-secret");
        if (await secrets.SecretExistsAsync(secretName, ct))
            config = config with { ClientSecret = await secrets.GetSecretAsync(secretName, ct) };
        var provider = Find(ssoProviders, config.Provider);
        await provider.ConfigureAsync(config, ct);
        var result = await provider.TestHandshakeAsync(ct);
        node["connectionTested"] = result.Success;
        node["clientSecret"] = null;
        PutStep(state, 3, node);
        await states.SaveState(token, state, ct);
        return result;
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

public sealed class EntraSsoProvider(IHttpClientFactory clients, AzureAdIdentityService azureAd) : SsoProviderBase("entra")
{
    public override async Task<HandshakeResult> TestHandshakeAsync(CancellationToken ct = default)
    {
        var config = RequiredConfig();
        if (string.IsNullOrWhiteSpace(config.TenantId) || string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.ClientSecret))
            return new(false, "Tenant ID, client ID and client secret are required.", "Copy the values from the Microsoft Entra app registration.");
        try
        {
            var discovery = $"https://login.microsoftonline.com/{Uri.EscapeDataString(config.TenantId)}/v2.0/.well-known/openid-configuration";
            using var response = await clients.CreateClient("wizard-providers").GetAsync(discovery, ct);
            if (!response.IsSuccessStatusCode)
                return new(false, $"Microsoft discovery returned HTTP {(int)response.StatusCode}.", "Verify the tenant ID and outbound HTTPS access.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!json.RootElement.TryGetProperty("authorization_endpoint", out _))
                return new(false, "Microsoft discovery response is incomplete.", "Verify the tenant is an active Microsoft Entra tenant.");
            var loginUrl = await azureAd.GetSsoLoginUrlAsync(config.TenantId, config.AcsUrl ?? "/api/identity/entra/callback", ct);
            return new(true, LoginUrl: loginUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(false, ex.Message, "Verify DNS, outbound HTTPS access, and the Entra tenant ID.");
        }
    }
}

public sealed class GoogleWorkspaceSsoProvider(IHttpClientFactory clients) : SsoProviderBase("google")
{
    public override async Task<HandshakeResult> TestHandshakeAsync(CancellationToken ct = default)
    {
        var config = RequiredConfig();
        if (string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.ClientSecret) || string.IsNullOrWhiteSpace(config.Domain))
            return new(false, "Client ID, client secret and Workspace domain are required.", "Create an OAuth 2.0 web application in Google Cloud Console.");
        try
        {
            using var response = await clients.CreateClient("wizard-providers").GetAsync("https://accounts.google.com/.well-known/openid-configuration", ct);
            if (!response.IsSuccessStatusCode)
                return new(false, $"Google discovery returned HTTP {(int)response.StatusCode}.", "Verify outbound HTTPS access.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return json.RootElement.TryGetProperty("authorization_endpoint", out var endpoint)
                ? new(true, LoginUrl: endpoint.GetString())
                : new(false, "Google discovery response is incomplete.", "Retry after checking Google service availability.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(false, ex.Message, "Verify DNS and outbound HTTPS access.");
        }
    }
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
