using AegisCore.Application.AI;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.AI;

public class InMemoryVectorStore : IVectorStore
{
    private readonly Dictionary<string, List<VectorChunk>> _collections = new();
    private readonly ILogger<InMemoryVectorStore> _logger;
    private readonly object _lock = new();

    public InMemoryVectorStore(ILogger<InMemoryVectorStore> logger)
    {
        _logger = logger;
    }

    public Task IndexAsync(string collection, string id, float[] embedding, Dictionary<string, object?> metadata, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_collections.ContainsKey(collection))
                _collections[collection] = new List<VectorChunk>();

            _collections[collection].RemoveAll(c => c.Id == id);

            _collections[collection].Add(new VectorChunk
            {
                Id = id,
                Embedding = embedding,
                Metadata = metadata
            });
        }

        _logger.LogDebug("Indexed chunk {ChunkId} in collection {Collection}", id, collection);
        return Task.CompletedTask;
    }

    public Task IndexBatchAsync(string collection, IEnumerable<(string Id, float[] Embedding, Dictionary<string, object?> Metadata)> items, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_collections.ContainsKey(collection))
                _collections[collection] = new List<VectorChunk>();

            foreach (var (id, embedding, metadata) in items)
            {
                _collections[collection].RemoveAll(c => c.Id == id);
                _collections[collection].Add(new VectorChunk
                {
                    Id = id,
                    Embedding = embedding,
                    Metadata = metadata
                });
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorChunk>> QueryAsync(string collection, float[] queryEmbedding, int topK = 5, Dictionary<string, object?>? filter = null, CancellationToken cancellationToken = default)
    {
        List<VectorChunk> results;

        lock (_lock)
        {
            if (!_collections.TryGetValue(collection, out var chunks))
                return Task.FromResult<IReadOnlyList<VectorChunk>>(Array.Empty<VectorChunk>());

            var scored = chunks
                .Select(chunk => new VectorChunk
                {
                    Id = chunk.Id,
                    Embedding = chunk.Embedding,
                    Metadata = chunk.Metadata,
                    Score = CosineSimilarity(queryEmbedding, chunk.Embedding)
                });

            if (filter != null)
            {
                scored = scored.Where(chunk =>
                    filter.All(f => chunk.Metadata.ContainsKey(f.Key) &&
                                    chunk.Metadata[f.Key]?.ToString() == f.Value?.ToString()));
            }

            results = scored
                .OrderByDescending(c => c.Score)
                .Take(topK)
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<VectorChunk>>(results);
    }

    public Task DeleteAsync(string collection, string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_collections.TryGetValue(collection, out var chunks))
                chunks.RemoveAll(c => c.Id == id);
        }

        return Task.CompletedTask;
    }

    public Task<bool> CollectionExistsAsync(string collection, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_collections.ContainsKey(collection));
        }
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;

        var dotProduct = 0f;
        var normA = 0f;
        var normB = 0f;

        for (var i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0f || normB == 0f) return 0f;
        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }
}
