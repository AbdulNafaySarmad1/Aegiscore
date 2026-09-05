using AegisCore.Application.ConfigCenter;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Config;

public class TenantConfigService
{
    private readonly IConfigurationCenterService _configCenter;
    private readonly ILogger<TenantConfigService> _logger;

    public TenantConfigService(IConfigurationCenterService configCenter, ILogger<TenantConfigService> logger)
    {
        _configCenter = configCenter; _logger = logger;
    }

    public string GetUniversityName(Guid? tenantId = null) => Resolve("identity.university_name", DefaultTenantConfigTemplate.DefaultUniversityName);
    public string GetUniversityShortName(Guid? tenantId = null) => Resolve("identity.university_short_name", DefaultTenantConfigTemplate.DefaultUniversityShortName);
    public string GetNoReplyEmail(Guid? tenantId = null) => Resolve("email.no_reply", DefaultTenantConfigTemplate.DefaultNoReplyEmail);
    public string GetSupportEmail(Guid? tenantId = null) => Resolve("email.support", DefaultTenantConfigTemplate.DefaultSupportEmail);
    public string GetSmtpHost(Guid? tenantId = null) => Resolve("email.smtp_host", DefaultTenantConfigTemplate.DefaultSmtpHost);
    public string GetSmtpPort(Guid? tenantId = null) => Resolve("email.smtp_port", DefaultTenantConfigTemplate.DefaultSmtpPort);
    public string GetFromAddress(Guid? tenantId = null) => Resolve("email.from_address", DefaultTenantConfigTemplate.DefaultFromAddress);
    public string GetFromName(Guid? tenantId = null) => Resolve("email.from_name", DefaultTenantConfigTemplate.DefaultFromName);
    public bool GetSslEnabled(Guid? tenantId = null) => ResolveBool("email.ssl_enabled", DefaultTenantConfigTemplate.DefaultSslEnabled);
    public string GetDatabaseName(Guid? tenantId = null) => Resolve("db.name", DefaultTenantConfigTemplate.DefaultDatabaseName);
    public string GetPostgresConnectionString(Guid? tenantId = null) => Resolve("db.connection_string", DefaultTenantConfigTemplate.DefaultPostgresConnectionString);
    public string GetElasticsearchUrl(Guid? tenantId = null) => Resolve("search.elasticsearch_url", DefaultTenantConfigTemplate.DefaultElasticsearchUrl);
    public string GetSearchIndexPrefix(Guid? tenantId = null) => Resolve("search.index_prefix", DefaultTenantConfigTemplate.DefaultSearchIndexPrefix);
    public string GetRedisConnection(Guid? tenantId = null) => Resolve("cache.redis", DefaultTenantConfigTemplate.DefaultRedisConnection);
    public string GetSamlBaseUrl(Guid? tenantId = null) => Resolve("identity.saml_base_url", DefaultTenantConfigTemplate.DefaultSamlBaseUrl);
    public string GetRaastBaseUrl(Guid? tenantId = null) => Resolve("payment.raast_base_url", DefaultTenantConfigTemplate.DefaultRaastBaseUrl);
    public string GetOpenAiBaseUrl(Guid? tenantId = null) => Resolve("ai.openai_base_url", DefaultTenantConfigTemplate.DefaultOpenAiBaseUrl);
    public string GetOllamaBaseUrl(Guid? tenantId = null) => Resolve("ai.ollama_base_url", DefaultTenantConfigTemplate.DefaultOllamaBaseUrl);
    public string GetOpenAiModel(Guid? tenantId = null) => Resolve("ai.openai_model", DefaultTenantConfigTemplate.DefaultOpenAiModel);
    public string GetOllamaModel(Guid? tenantId = null) => Resolve("ai.ollama_model", DefaultTenantConfigTemplate.DefaultOllamaModel);
    public string GetEmbeddingModel(Guid? tenantId = null) => Resolve("ai.embedding_model", DefaultTenantConfigTemplate.DefaultEmbeddingModel);
    public string GetOllamaEmbeddingModel(Guid? tenantId = null) => Resolve("ai.ollama_embedding_model", DefaultTenantConfigTemplate.DefaultOllamaEmbeddingModel);
    public string GetDefaultAiProvider(Guid? tenantId = null) => Resolve("ai.provider", DefaultTenantConfigTemplate.DefaultAiProvider);
    public decimal GetMinCgpa(Guid? tenantId = null) => ResolveDecimal("academic.min_cgpa", DefaultTenantConfigTemplate.MinCGPA);
    public decimal GetAcademicProbationCgpa(Guid? tenantId = null) => ResolveDecimal("academic.probation_cgpa", DefaultTenantConfigTemplate.AcademicProbationCgpa);
    public int GetRateLimitPerMinute(Guid? tenantId = null) => ResolveInt("api.rate_limit_per_minute", DefaultTenantConfigTemplate.DefaultRateLimitPerMinute);
    public int GetDefaultTimeoutSeconds(Guid? tenantId = null) => ResolveInt("system.timeout_seconds", DefaultTenantConfigTemplate.DefaultTimeoutSeconds);
    public int GetRetryAfterSeconds(Guid? tenantId = null) => ResolveInt("api.retry_after_seconds", DefaultTenantConfigTemplate.DefaultRetryAfterSeconds);
    public int GetRateLimitBurst(Guid? tenantId = null) => ResolveInt("api.rate_limit_burst", DefaultTenantConfigTemplate.DefaultRateLimitBurst);
    public string GetTimezone(Guid? tenantId = null) => Resolve("locale.timezone", DefaultTenantConfigTemplate.DefaultTimezone);
    public string GetCurrency(Guid? tenantId = null) => Resolve("locale.currency", DefaultTenantConfigTemplate.DefaultCurrency);
    public string GetLocale(Guid? tenantId = null) => Resolve("locale.language", DefaultTenantConfigTemplate.DefaultLanguage);
    public string GetPaymentGateway(Guid? tenantId = null) => Resolve("payment.default_gateway", DefaultTenantConfigTemplate.DefaultPaymentGateway);
    public string GetAccreditorName(Guid? tenantId = null) => Resolve("academic.accreditor_name", DefaultTenantConfigTemplate.DefaultAccreditorName);
    public string GetIdentityDocumentName(Guid? tenantId = null) => Resolve("identity.document_name", DefaultTenantConfigTemplate.DefaultIdentityDocumentName);

    private string Resolve(string key, string fallback)
    {
        try { var c = _configCenter.GetConfigAsync(key).GetAwaiter().GetResult(); return c.Value?.ToString() ?? fallback; }
        catch { return fallback; }
    }

    private bool ResolveBool(string key, bool fallback)
    {
        try { var c = _configCenter.GetConfigAsync(key).GetAwaiter().GetResult(); return bool.TryParse(c.Value?.ToString(), out var b) ? b : fallback; }
        catch { return fallback; }
    }

    private decimal ResolveDecimal(string key, decimal fallback)
    {
        try { var c = _configCenter.GetConfigAsync(key).GetAwaiter().GetResult(); return decimal.TryParse(c.Value?.ToString(), out var d) ? d : fallback; }
        catch { return fallback; }
    }

    private int ResolveInt(string key, int fallback)
    {
        try { var c = _configCenter.GetConfigAsync(key).GetAwaiter().GetResult(); return int.TryParse(c.Value?.ToString(), out var i) ? i : fallback; }
        catch { return fallback; }
    }

    private async Task<string> ResolveAsync(string key, string fallback)
    {
        try { var c = await _configCenter.GetConfigAsync(key); return c.Value?.ToString() ?? fallback; }
        catch { return fallback; }
    }

    private async Task<bool> ResolveAsyncBool(string key, bool fallback)
    {
        try { var c = await _configCenter.GetConfigAsync(key); return bool.TryParse(c.Value?.ToString(), out var b) ? b : fallback; }
        catch { return fallback; }
    }

    private async Task<decimal> ResolveAsyncDecimal(string key, decimal fallback)
    {
        try { var c = await _configCenter.GetConfigAsync(key); return decimal.TryParse(c.Value?.ToString(), out var d) ? d : fallback; }
        catch { return fallback; }
    }

    private async Task<int> ResolveAsyncInt(string key, int fallback)
    {
        try { var c = await _configCenter.GetConfigAsync(key); return int.TryParse(c.Value?.ToString(), out var i) ? i : fallback; }
        catch { return fallback; }
    }
}
