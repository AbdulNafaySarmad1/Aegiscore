using System.Text;
using System.Text.Json;
using AegisCore.Application.AI;
using AegisCore.Contracts.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AegisCore.Infrastructure.AI;

public class SupportBotQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly LlmProviderFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SupportBotQueryService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SupportBotQueryService(
        IUnitOfWork unitOfWork,
        LlmProviderFactory factory,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<SupportBotQueryService> logger)
    {
        _unitOfWork = unitOfWork;
        _factory = factory;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<BotResponse> AnswerQueryAsync(
        string userQuestion,
        Guid studentId,
        Guid tenantId)
    {
        _logger.LogInformation("Processing query from student {StudentId}: {Question}", studentId, userQuestion);

        var startTime = DateTime.UtcNow;

        // 1. Generate embedding for question
        var embedder = _factory.GetEmbeddingProvider(tenantId);
        var questionEmbedding = await embedder.GetEmbeddingAsync(userQuestion);

        // 2. Query vector store
        var vectorStore = GetVectorStore(tenantId);
        var retrievedChunks = await vectorStore.QueryAsync(
            tenantId.ToString(), questionEmbedding, 5, null);

        // 3. Build context
        var contextBuilder = new StringBuilder();
        foreach (var chunk in retrievedChunks)
        {
            if (chunk.Metadata.TryGetValue("sourceText", out var sourceText))
            {
                contextBuilder.AppendLine(sourceText?.ToString());
            }
        }
        var context = contextBuilder.ToString();

        // 4. Get university name
        var universityName = await GetUniversityNameAsync(tenantId);

        // 5. Build system prompt
        var llm = _factory.GetLlmProvider(tenantId);
        var systemPrompt = SystemPromptTemplate.Generate(universityName, llm.ModelName, context);

        // 6. Call LLM
        var options = new LlmOptions { MaxTokens = 1024, Temperature = 0.3f };
        var response = await llm.CompleteAsync(systemPrompt, userQuestion, options);

        // 7. Parse response
        var (answerText, suggestedAction, isEscalated) = ParseResponse(response);

        var responseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

        return new BotResponse
        {
            AnswerText = answerText,
            SuggestedAction = suggestedAction,
            IsEscalated = isEscalated,
            ResponseTimeMs = responseTimeMs
        };
    }

    private IVectorStore GetVectorStore(Guid tenantId)
    {
        // In production, resolve from DI based on tenant config
        return new InMemoryVectorStore(
            _serviceProvider.GetRequiredService<ILogger<InMemoryVectorStore>>());
    }

    private async Task<string> GetUniversityNameAsync(Guid tenantId)
    {
        var connectionString = _configuration.GetConnectionString("SupabasePostgres");
        if (string.IsNullOrEmpty(connectionString)) return "the University";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = "SELECT university_name FROM tenant_configs WHERE tenant_id = @tenantId";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tenantId", tenantId);

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() ?? "the University";
    }

    private (string answerText, string suggestedAction, bool isEscalated) ParseResponse(string response)
    {
        var isEscalated = response.StartsWith("ESCALATE:", StringComparison.OrdinalIgnoreCase);
        var suggestedAction = "NONE";

        var actionIndex = response.LastIndexOf("[ACTION:", StringComparison.OrdinalIgnoreCase);
        if (actionIndex >= 0)
        {
            var actionStart = actionIndex + 8;
            var actionEnd = response.IndexOf("]", actionStart);
            if (actionEnd > actionStart)
            {
                suggestedAction = response.Substring(actionStart, actionEnd - actionStart).Trim();
            }
            response = response.Substring(0, actionIndex).Trim();
        }

        return (response, suggestedAction, isEscalated);
    }
}

public class BotResponse
{
    public string AnswerText { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = "NONE";
    public bool IsEscalated { get; set; }
    public double ResponseTimeMs { get; set; }
}
