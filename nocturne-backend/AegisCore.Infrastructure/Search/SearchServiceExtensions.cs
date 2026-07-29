using AegisCore.Application.Search;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nest;

namespace AegisCore.Infrastructure.Search;

public static class SearchServiceExtensions
{
    public static IServiceCollection AddEnterpriseSearch(this IServiceCollection services, IConfiguration configuration)
    {
        var esUrl = configuration["Search:ElasticsearchUrl"];
        if (string.IsNullOrWhiteSpace(esUrl) || esUrl.StartsWith("${", StringComparison.Ordinal))
            esUrl = DefaultTenantConfigTemplate.DefaultElasticsearchUrl;
        var settings = new ConnectionSettings(new Uri(esUrl))
            .DefaultIndex(configuration["Search:IndexPrefix"] ?? DefaultTenantConfigTemplate.DefaultSearchIndexPrefix)
            .EnableDebugMode()
            .PrettyJson();

        services.TryAddSingleton<IElasticClient>(new ElasticClient(settings));

        services.TryAddSingleton(typeof(ISearchIndex<>), typeof(ElasticsearchIndex<>));
        services.TryAddScoped<ISearchService, SearchService>();

        return services;
    }
}
