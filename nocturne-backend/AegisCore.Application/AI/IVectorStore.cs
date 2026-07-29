namespace AegisCore.Application.AI;

public interface IVectorStore
{
    Task IndexAsync(string collection, string id, float[] embedding, Dictionary<string, object?> metadata, CancellationToken cancellationToken = default);
    Task IndexBatchAsync(string collection, IEnumerable<(string Id, float[] Embedding, Dictionary<string, object?> Metadata)> items, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VectorChunk>> QueryAsync(string collection, float[] queryEmbedding, int topK = 5, Dictionary<string, object?>? filter = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(string collection, string id, CancellationToken cancellationToken = default);
    Task<bool> CollectionExistsAsync(string collection, CancellationToken cancellationToken = default);
}
