using AegisCore.Application.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.AI;

public static class AiPlatformServiceExtensions
{
    public static IServiceCollection AddAiPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<LlmProviderFactory>();

        services.TryAddSingleton<OpenAiLlmProvider>();
        services.TryAddSingleton<OllamaLlmProvider>();
        services.TryAddSingleton<OpenAiEmbeddingProvider>();
        services.TryAddSingleton<OllamaEmbeddingProvider>();
        services.TryAddSingleton<IDocumentOcrService, DocumentOcrService>();

        services.TryAddScoped<StudentAiAgent>();
        services.TryAddScoped<DocumentAiAgent>();
        services.TryAddScoped<SupportBotQueryService>();

        services.TryAddScoped<IAiAgent>(sp => sp.GetRequiredService<StudentAiAgent>());

        return services;
    }
}
