namespace AegisCore.Application.AI;

public interface ILlmProvider
{
    string ModelName { get; }
    string ProviderName { get; }
    Task<string> CompleteAsync(string systemPrompt, string userMessage, LlmOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userMessage, LlmOptions? options = null, CancellationToken cancellationToken = default);
}
