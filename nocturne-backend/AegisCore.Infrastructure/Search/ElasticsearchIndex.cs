using AegisCore.Application.Search;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;

namespace AegisCore.Infrastructure.Search;

public class ElasticsearchIndex<T> : ISearchIndex<T> where T : class
{
    private readonly IElasticClient _client;
    private readonly string _indexName;
    private readonly ILogger<ElasticsearchIndex<T>> _logger;

    public ElasticsearchIndex(IElasticClient client, IConfiguration configuration, ILogger<ElasticsearchIndex<T>> logger)
    {
        _client = client;
        _indexName = $"{configuration["Search:IndexPrefix"] ?? DefaultTenantConfigTemplate.DefaultSearchIndexPrefix}_{typeof(T).Name.ToLowerInvariant()}";
        _logger = logger;
    }

    public async Task IndexAsync(T document, CancellationToken cancellationToken = default)
    {
        var response = await _client.IndexAsync(document, i => i.Index(_indexName), cancellationToken);
        if (!response.IsValid)
            _logger.LogWarning("Index failed for {Index}: {Error}", _indexName, response.ServerError?.Error?.Reason);
    }

    public async Task IndexBatchAsync(IEnumerable<T> documents, CancellationToken cancellationToken = default)
    {
        var response = await _client.BulkAsync(b => b.Index(_indexName).IndexMany(documents), cancellationToken);
        if (response.Errors)
            _logger.LogWarning("Batch index had errors for {Index}", _indexName);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync<T>(id, d => d.Index(_indexName), cancellationToken);
        if (!response.IsValid)
            _logger.LogWarning("Delete failed for {Index}/{Id}", _indexName, id);
    }

    public async Task<SearchResult<T>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var searchDescriptor = new SearchDescriptor<T>()
            .Index(_indexName)
            .From((query.Page - 1) * query.PageSize)
            .Size(query.PageSize)
            .Query(q =>
            {
                var descriptor = new QueryContainerDescriptor<T>();
                QueryContainer container = new MatchQuery
                {
                    Field = "_all",
                    Query = query.Query
                };

                if (query.Filters != null)
                {
                    foreach (var filter in query.Filters)
                    {
                        container &= new TermQuery { Field = filter.Field, Value = filter.Value };
                    }
                }

                return container;
            });

        if (!string.IsNullOrEmpty(query.SortBy))
        {
            searchDescriptor.Sort(s => query.SortDescending
                ? s.Field(new Field(query.SortBy), SortOrder.Descending)
                : s.Field(new Field(query.SortBy), SortOrder.Ascending));
        }

        var response = await _client.SearchAsync<T>(searchDescriptor, cancellationToken);

        return new SearchResult<T>
        {
            TotalHits = response.Total,
            Page = query.Page,
            PageSize = query.PageSize,
            Hits = response.Documents.ToList()
        };
    }

    public async Task RebuildIndexAsync(IEnumerable<T> documents, CancellationToken cancellationToken = default)
    {
        var exists = await _client.Indices.ExistsAsync(_indexName, ct: cancellationToken);
        if (exists.Exists)
            await _client.Indices.DeleteAsync(_indexName, ct: cancellationToken);

        await _client.Indices.CreateAsync(_indexName, ct: cancellationToken);
        await IndexBatchAsync(documents, cancellationToken);

        _logger.LogInformation("Index rebuilt: {Index} with {Count} documents", _indexName, documents.Count());
    }

    public async Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _client.Indices.ExistsAsync(_indexName, ct: cancellationToken);
        return exists.Exists;
    }
}
