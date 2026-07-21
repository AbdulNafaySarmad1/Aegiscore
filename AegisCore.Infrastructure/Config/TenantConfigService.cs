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

    public string GetUniversityName(Guid? tenantId = null) => ResolveAsync("identity.university_name", DefaultTenantConfigTemplate.DefaultUniversityName).GetAwaiter().GetResult();
    public string GetUniversityShortName(Guid? tenantId = null) => ResolveAsync("identity.university_short_name", DefaultTenantConfigTemplate.DefaultUniversityShortName).GetAwaiter().GetResult();
    public string GetNoReplyEmail(Guid? tenantId = null) => ResolveAsync("email.no_reply", DefaultTenantConfigTemplate.DefaultNoReplyEmail).GetAwaiter().GetResult();
    public string GetSupportEmail(Guid? tenantId = null) => ResolveAsync("email.support", DefaultTenantConfigTemplate.DefaultSupportEmail).GetAwaiter().GetResult();
    public string GetSmtpHost(Guid? tenantId = null) => ResolveAsync("email.smtp_host", DefaultTenantConfigTemplate.DefaultSmtpHost).GetAwaiter().GetResult();
    public string GetSmtpPort(Guid? tenantId = null) => ResolveAsync("email.smtp_port", DefaultTenantConfigTemplate.DefaultSmtpPort).GetAwaiter().GetResult();
    public string GetFromAddress(Guid? tenantId = null) => ResolveAsync("email.from_address", DefaultTenantConfigTemplate.DefaultFromAddress).GetAwaiter().GetResult();
    public string GetFromName(Guid? tenantId = null) => ResolveAsync("email.from_name", DefaultTenantConfigTemplate.DefaultFromName).GetAwaiter().GetResult();
    public bool GetSslEnabled(Guid? tenantId = null) => ResolveAsyncBool("email.ssl_enabled", DefaultTenantConfigTemplate.DefaultSslEnabled).GetAwaiter().GetResult();
    public string GetDatabaseName(Guid? tenantId = null) => ResolveAsync("db.name", DefaultTenantConfigTemplate.DefaultDatabaseName).GetAwaiter().GetResult();
    public string GetPostgresConnectionString(Guid? tenantId = null) => ResolveAsync("db.connection_string", DefaultTenantConfigTemplate.DefaultPostgresConnectionString).GetAwaiter().GetResult();
    public string GetElasticsearchUrl(Guid? tenantId = null) => ResolveAsync("search.elasticsearch_url", DefaultTenantConfigTemplate.DefaultElasticsearchUrl).GetAwaiter().GetResult();
    public string GetSearchIndexPrefix(Guid? tenantId = null) => ResolveAsync("search.index_prefix", DefaultTenantConfigTemplate.DefaultSearchIndexPrefix).GetAwaiter().GetResult();
    public string GetRedisConnection(Guid? tenantId = null) => ResolveAsync("cache.redis", DefaultTenantConfigTemplate.DefaultRedisConnection).GetAwaiter().GetResult();
    public string GetSamlBaseUrl(Guid? tenantId = null) => ResolveAsync("identity.saml_base_url", DefaultTenantConfigTemplate.DefaultSamlBaseUrl).GetAwaiter().GetResult();
    public string GetRaastBaseUrl(Guid? tenantId = null) => ResolveAsync("payment.raast_base_url", DefaultTenantConfigTemplate.DefaultRaastBaseUrl).GetAwaiter().GetResult();
    public string GetOpenAiBaseUrl(Guid? tenantId = null) => ResolveAsync("ai.openai_base_url", DefaultTenantConfigTemplate.DefaultOpenAiBaseUrl).GetAwaiter().GetResult();
    public string GetOllamaBaseUrl(Guid? tenantId = null) => ResolveAsync("ai.ollama_base_url", DefaultTenantConfigTemplate.DefaultOllamaBaseUrl).GetAwaiter().GetResult();
    public string GetOpenAiModel(Guid? tenantId = null) => ResolveAsync("ai.openai_model", DefaultTenantConfigTemplate.DefaultOpenAiModel).GetAwaiter().GetResult();
    public string GetOllamaModel(Guid? tenantId = null) => ResolveAsync("ai.ollama_model", DefaultTenantConfigTemplate.DefaultOllamaModel).GetAwaiter().GetResult();
    public string GetEmbeddingModel(Guid? tenantId = null) => ResolveAsync("ai.embedding_model", DefaultTenantConfigTemplate.DefaultEmbeddingModel).GetAwaiter().GetResult();
    public string GetOllamaEmbeddingModel(Guid? tenantId = null) => ResolveAsync("ai.ollama_embedding_model", DefaultTenantConfigTemplate.DefaultOllamaEmbeddingModel).GetAwaiter().GetResult();
    public string GetDefaultAiProvider(Guid? tenantId = null) => ResolveAsync("ai.provider", DefaultTenantConfigTemplate.DefaultAiProvider).GetAwaiter().GetResult();
    public decimal GetMinCgpa(Guid? tenantId = null) => ResolveAsyncDecimal("academic.min_cgpa", DefaultTenantConfigTemplate.MinCGPA).GetAwaiter().GetResult();
    public decimal GetAcademicProbationCgpa(Guid? tenantId = null) => ResolveAsyncDecimal("academic.probation_cgpa", DefaultTenantConfigTemplate.AcademicProbationCgpa).GetAwaiter().GetResult();
    public int GetRateLimitPerMinute(Guid? tenantId = null) => ResolveAsyncInt("api.rate_limit_per_minute", DefaultTenantConfigTemplate.DefaultRateLimitPerMinute).GetAwaiter().GetResult();
    public int GetDefaultTimeoutSeconds(Guid? tenantId = null) => ResolveAsyncInt("system.timeout_seconds", DefaultTenantConfigTemplate.DefaultTimeoutSeconds).GetAwaiter().GetResult();
    public int GetRetryAfterSeconds(Guid? tenantId = null) => ResolveAsyncInt("api.retry_after_seconds", DefaultTenantConfigTemplate.DefaultRetryAfterSeconds).GetAwaiter().GetResult();
    public int GetRateLimitBurst(Guid? tenantId = null) => ResolveAsyncInt("api.rate_limit_burst", DefaultTenantConfigTemplate.DefaultRateLimitBurst).GetAwaiter().GetResult();
    public string GetTimezone(Guid? tenantId = null) => ResolveAsync("locale.timezone", DefaultTenantConfigTemplate.DefaultTimezone).GetAwaiter().GetResult();
    public string GetCurrency(Guid? tenantId = null) => ResolveAsync("locale.currency", DefaultTenantConfigTemplate.DefaultCurrency).GetAwaiter().GetResult();

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
