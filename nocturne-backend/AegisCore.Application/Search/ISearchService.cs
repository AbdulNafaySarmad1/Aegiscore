namespace AegisCore.Application.Search;

public interface ISearchService
{
    Task<UnifiedSearchResponse> SearchAsync(string query, UnifiedSearchRequest request, CancellationToken cancellationToken = default);
    Task IndexEntityAsync(string entityType, object entity, CancellationToken cancellationToken = default);
    Task DeleteEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
    Task RebuildAllIndicesAsync(CancellationToken cancellationToken = default);
}
