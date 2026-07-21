using AegisCore.Application.AI;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.AI;

public class LlmProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public LlmProviderFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public ILlmProvider GetLlmProvider(Guid tenantId)
    {
        var provider = _configuration[$"AI:Tenants:{tenantId}:LlmProvider"]
            ?? _configuration["AI:Provider"]
            ?? DefaultTenantConfigTemplate.DefaultAiProvider;

        return provider.ToLowerInvariant() switch
        {
            "ollama" => _serviceProvider.GetRequiredService<OllamaLlmProvider>(),
            _ => _serviceProvider.GetRequiredService<OpenAiLlmProvider>()
        };
    }

    public IEmbeddingProvider GetEmbeddingProvider(Guid tenantId)
    {
        var provider = _configuration[$"AI:Tenants:{tenantId}:EmbeddingProvider"]
            ?? _configuration["AI:EmbeddingProvider"]
            ?? DefaultTenantConfigTemplate.DefaultAiProvider;

        return provider.ToLowerInvariant() switch
        {
            "ollama" => _serviceProvider.GetRequiredService<OllamaEmbeddingProvider>(),
            _ => _serviceProvider.GetRequiredService<OpenAiEmbeddingProvider>()
        };
    }
}
