namespace AegisCore.Application.AI;

public interface IEmbeddingProvider
{
    string ModelName { get; }
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
