using System.Runtime.CompilerServices;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AegisCore.Application.AI;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.AI;

public class OpenAiLlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiLlmProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public string ModelName { get; }
    public string ProviderName => "OpenAI";

    public OpenAiLlmProvider(IConfiguration configuration, ILogger<OpenAiLlmProvider> logger)
    {
        _logger = logger;
        ModelName = configuration["AI_MODEL"] ?? DefaultTenantConfigTemplate.DefaultOpenAiModel;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var apiKey = configuration["AI_API_KEY"];
        var baseUrl = configuration["AI_ENDPOINT"] ?? "https://api.openai.com/v1/";
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, LlmOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new LlmOptions();

        var requestBody = new
        {
            model = ModelName,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            max_tokens = options.MaxTokens,
            temperature = options.Temperature,
            top_p = options.TopP,
            frequency_penalty = options.FrequencyPenalty,
            presence_penalty = options.PresencePenalty,
            stop = options.StopSequences?.ToArray()
        };

        var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("OpenAI request: model={Model}, tokens={MaxTokens}, temp={Temp}", ModelName, options.MaxTokens, options.Temperature);

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var completion = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return completion ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userMessage, LlmOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= new LlmOptions();

        var requestBody = new
        {
            model = ModelName,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            max_tokens = options.MaxTokens,
            temperature = options.Temperature,
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions") { Content = content };
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
            var data = line.Substring(6);
            if (data == "[DONE]") break;

            string? chunk = null;
            try
            {
                using var doc = JsonDocument.Parse(data);
                var delta = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");
                if (delta.TryGetProperty("content", out var contentElement))
                    chunk = contentElement.GetString();
            }
            catch (JsonException) { }
            if (chunk != null) yield return chunk;
        }
    }
}
