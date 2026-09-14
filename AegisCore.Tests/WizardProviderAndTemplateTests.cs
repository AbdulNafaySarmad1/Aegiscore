using System.Net;
using System.Text;
using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Tests;

public sealed class WizardProviderAndTemplateTests
{
    private const string Token = "provider-test-bootstrap-token-with-at-least-32-bytes";

    [Fact]
    public async Task Wizard_Uses_Configured_Login_Tenant_And_Rejects_Mismatched_Resume()
    {
        await using var fixture = await WizardFixture.CreateAsync();
        var tenant = Guid.NewGuid();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TENANT_ID"] = tenant.ToString() }).Build();
        var sessions = new WizardSessionService(fixture.Db, new AegisCore.Infrastructure.Security.PasswordHashingService(), fixture.States, configuration);
        var state = await sessions.GetOrCreateAsync(Token + "-bound", null, default);
        Assert.Equal(tenant, state.TenantId);
        Assert.Equal(tenant, (await fixture.States.InitializeTenantAtStepOneAsync(Token + "-bound")).TenantId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => sessions.GetOrCreateAsync(Token, null, default));
    }

    [Fact]
    public async Task Step3_SsoConfiguration_StoresSecretOutsideWizardPayload_AndTestsHandshake()
    {
        await using var fixture = await WizardFixture.CreateAsync();
        var sso = new RecordingSsoProvider();
        var service = new WizardProviderConfigurationService(fixture.States, fixture.Secrets, [sso], [new RecordingAiProvider()]);

        await service.ConfigureSsoAsync(Token, new SsoConfigDto("test-sso", ClientId: "client", ClientSecret: "sso-secret"));
        var persisted = await fixture.States.ResumeWizardAsync(Token);
        Assert.DoesNotContain("sso-secret", persisted.StepData, StringComparison.Ordinal);
        Assert.Equal("sso-secret", await fixture.Secrets.GetSecretAsync(SecretName("sso-client-secret")));

        var result = await service.TestSsoAsync(Token);
        Assert.True(result.Success);
        Assert.Equal("sso-secret", sso.LastConfig?.ClientSecret);
        Assert.Contains("\"connectionTested\":true", (await fixture.States.ResumeWizardAsync(Token)).StepData, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Step5_AiConfiguration_StoresApiKeyOutsideWizardPayload_AndTestsHello()
    {
        await using var fixture = await WizardFixture.CreateAsync();
        var ai = new RecordingAiProvider();
        var service = new WizardProviderConfigurationService(fixture.States, fixture.Secrets, [new RecordingSsoProvider()], [ai]);

        await service.ConfigureAiAsync(Token, new AiConfigDto("test-ai", "test-model", "https://ai.example", "ai-secret"));
        Assert.DoesNotContain("ai-secret", (await fixture.States.ResumeWizardAsync(Token)).StepData, StringComparison.Ordinal);
        Assert.Equal("ai-secret", await fixture.Secrets.GetSecretAsync(SecretName("ai-api-key")));

        var result = await service.TestAiAsync(Token);
        Assert.True(result.Success);
        Assert.Equal("ai-secret", ai.LastConfig?.ApiKey);
        Assert.Equal("Hello", ai.LastPrompt);
    }

    [Fact]
    public async Task Step1_DocumentTemplate_SanitizesHtml_ExtractsDesign_AndPersistsPreview()
    {
        await using var fixture = await WizardFixture.CreateAsync();
        var service = new DocumentTemplateService(fixture.Db, fixture.States);
        var source = "<style>:root{--brand:#123456} body{font-family:'Example Sans',sans-serif}</style><script>alert(1)</script><h1 onclick=\"bad()\">{{university}}</h1>{{records}}";

        var saved = await service.WriteAsync(Token, new DocumentTemplateWriteRequest("Transcript", source, "https://assets.example/logo.png"));
        Assert.Contains("#123456", saved.ExtractedPalette);
        Assert.Contains("Example Sans", saved.ExtractedFonts);
        Assert.DoesNotContain("<script", saved.PreviewHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", saved.PreviewHtml, StringComparison.OrdinalIgnoreCase);

        var preview = await service.PreviewAsync(Token);
        Assert.Equal(saved.Id, preview.Id);
        Assert.Contains("Systems Design", preview.PreviewHtml);
        Assert.Equal(1, await fixture.Db.DocumentTemplates.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task GenericSamlProvider_ValidatesSpInitiatedMetadata()
    {
        var provider = new GenericSamlSsoProvider(new FixedClientFactory(new HttpClient()));
        await provider.ConfigureAsync(new SsoConfigDto(
            "saml", MetadataXml: """
            <md:EntityDescriptor xmlns:md="urn:oasis:names:tc:SAML:2.0:metadata" entityID="https://idp.example/entity">
              <md:IDPSSODescriptor><md:SingleSignOnService Location="https://idp.example/sso" /></md:IDPSSODescriptor>
            </md:EntityDescriptor>
            """, EntityId: "https://sp.example/metadata", AcsUrl: "https://sp.example/acs"));
        var result = await provider.TestHandshakeAsync();
        Assert.True(result.Success);
        Assert.StartsWith("https://idp.example/sso?SAMLRequest=", result.LoginUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GraniteProvider_SendsHelloUsingBearerAuthentication()
    {
        var handler = new RecordingHandler("""{"results":[{"generated_text":"Hello back"}]}""");
        var provider = new GraniteProvider(new FixedClientFactory(new HttpClient(handler)));
        await provider.ConfigureAsync(new AiConfigDto("granite", "ibm/granite-13b-chat-v2", ApiKey: "iam-key"));
        var result = await provider.TestConnectionAsync();
        Assert.True(result.Success);
        Assert.Contains("Hello", handler.Body);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("iam-key", handler.AuthorizationParameter);
    }

    private static string SecretName(string suffix)
        => $"/wizard/{WizardStateService.HashToken(Token).ToLowerInvariant()}/{suffix}";

    private sealed class RecordingSsoProvider : ISsoProvider
    {
        public string Name => "test-sso";
        public SsoConfigDto? LastConfig { get; private set; }
        public Task ConfigureAsync(SsoConfigDto config, CancellationToken ct = default) { LastConfig = config; return Task.CompletedTask; }
        public Task<HandshakeResult> TestHandshakeAsync(CancellationToken ct = default)
            => Task.FromResult(new HandshakeResult(LastConfig?.ClientSecret == "sso-secret"));
    }

    private sealed class RecordingAiProvider : IAiSetupProvider
    {
        public string Name => "test-ai";
        public AiConfigDto? LastConfig { get; private set; }
        public string? LastPrompt { get; private set; }
        public Task ConfigureAsync(AiConfigDto config, CancellationToken ct = default) { LastConfig = config; return Task.CompletedTask; }
        public Task<AiConnectionResult> TestConnectionAsync(CancellationToken ct = default)
        {
            LastPrompt = "Hello";
            return Task.FromResult(new AiConnectionResult(LastConfig?.ApiKey == "ai-secret", "Hello back"));
        }
    }

    private sealed class FixedClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(string response) : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class WizardFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AegisDbContext Db { get; }
        public TestSecretService Secrets { get; } = new();
        public WizardStateService States { get; }

        private WizardFixture(SqliteConnection connection, AegisDbContext db)
        {
            _connection = connection;
            Db = db;
            States = WizardTestState.Create(db, Secrets);
        }

        public static async Task<WizardFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var fixture = new WizardFixture(connection, db);
            await fixture.States.GetOrCreateStateAsync(Token);
            await fixture.States.InitializeTenantAtStepOneAsync(Token);
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
