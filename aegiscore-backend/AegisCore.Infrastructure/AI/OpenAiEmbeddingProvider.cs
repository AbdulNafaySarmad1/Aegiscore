using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AegisCore.Application.AI;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.AI;

public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiEmbeddingProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ModelName { get; }

    public OpenAiEmbeddingProvider(IConfiguration configuration, ILogger<OpenAiEmbeddingProvider> logger)
    {
        _logger = logger;
        ModelName = configuration["AI:OpenAI:EmbeddingModel"] ?? DefaultTenantConfigTemplate.DefaultEmbeddingModel;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var apiKey = configuration["AI:OpenAI:ApiKey"];
        var baseUrl = configuration["AI:OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.Timeout = TimeSpan.FromMinutes(1);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var results = await GetEmbeddingsAsync(new[] { text }, cancellationToken);
        return results.Length > 0 ? results[0] : Array.Empty<float>();
    }

    public async Task<float[][]> GetEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textsList = texts.ToList();

        var requestBody = new
        {
            model = ModelName,
            input = textsList
        };

        var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("OpenAI embedding request: model={Model}, count={Count}", ModelName, textsList.Count);

        var response = await _httpClient.PostAsync("embeddings", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var dataArray = doc.RootElement.GetProperty("data");

        var embeddings = new List<float[]>();
        foreach (var item in dataArray.EnumerateArray())
        {
            var embedding = item.GetProperty("embedding")
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();
            embeddings.Add(embedding);
        }

        return embeddings.ToArray();
    }
}
