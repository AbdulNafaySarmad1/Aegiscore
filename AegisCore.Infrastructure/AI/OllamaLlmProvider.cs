using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AegisCore.Application.AI;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.AI;

public class OllamaLlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLlmProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ModelName { get; }
    public string ProviderName => "Ollama";

    public OllamaLlmProvider(IConfiguration configuration, ILogger<OllamaLlmProvider> logger)
    {
        _logger = logger;
        ModelName = configuration["AI:Ollama:Model"] ?? DefaultTenantConfigTemplate.DefaultOllamaModel;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var baseUrl = configuration["AI:Ollama:BaseUrl"] ?? "http://localhost:11434/";
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, LlmOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new LlmOptions();

        var combinedPrompt = $"[SYSTEM]\n{systemPrompt}\n\n[USER]\n{userMessage}\n\n[ASSISTANT]";

        var requestBody = new
        {
            model = ModelName,
            prompt = combinedPrompt,
            stream = false,
            options = new
            {
                num_predict = options.MaxTokens,
                temperature = options.Temperature,
                top_p = options.TopP
            }
        };

        var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Ollama request: model={Model}", ModelName);

        var response = await _httpClient.PostAsync("api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var completion = doc.RootElement.GetProperty("response").GetString();

        return completion ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userMessage, LlmOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new LlmOptions();

        var combinedPrompt = $"[SYSTEM]\n{systemPrompt}\n\n[USER]\n{userMessage}\n\n[ASSISTANT]";

        var requestBody = new
        {
            model = ModelName,
            prompt = combinedPrompt,
            stream = true,
            options = new
            {
                num_predict = options.MaxTokens,
                temperature = options.Temperature
            }
        };

        var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "api/generate") { Content = content };
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;
            string? chunk = null;
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("response", out var responseElement))
                    chunk = responseElement.GetString();
            }
            catch (JsonException) { }
            if (chunk != null) yield return chunk;
        }
    }
}
