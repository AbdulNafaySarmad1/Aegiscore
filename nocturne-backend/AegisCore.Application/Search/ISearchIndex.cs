namespace AegisCore.Application.Search;

public interface ISearchIndex<T> where T : class
{
    Task IndexAsync(T document, CancellationToken cancellationToken = default);
    Task IndexBatchAsync(IEnumerable<T> documents, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<SearchResult<T>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
    Task RebuildIndexAsync(IEnumerable<T> documents, CancellationToken cancellationToken = default);
    Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default);
}
