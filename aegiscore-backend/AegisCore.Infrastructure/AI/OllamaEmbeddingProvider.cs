using System.Text;
using System.Text.Json;
using AegisCore.Application.AI;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.AI;

public class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaEmbeddingProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ModelName { get; }

    public OllamaEmbeddingProvider(IConfiguration configuration, ILogger<OllamaEmbeddingProvider> logger)
    {
        _logger = logger;
        ModelName = configuration["AI:Ollama:EmbeddingModel"] ?? DefaultTenantConfigTemplate.DefaultOllamaEmbeddingModel;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var baseUrl = configuration["AI:Ollama:BaseUrl"] ?? "http://localhost:11434/";
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var requestBody = new { model = ModelName, input = text };

        var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/embeddings", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var embedding = doc.RootElement.GetProperty("embedding")
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();

        return embedding;
    }

    public async Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            var embedding = await GetEmbeddingAsync(text, cancellationToken);
            results.Add(embedding);
        }
        return results.ToArray();
    }
}
