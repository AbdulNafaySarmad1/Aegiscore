using System.Text.Json;
using AegisCore.Application.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.AI;

public class DocumentAiAgent : IAiAgent
{
    private readonly IDocumentOcrService _ocrService;
    private readonly LlmProviderFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DocumentAiAgent> _logger;

    public string AgentName => "Document AI";
    public string Domain => "Document Processing";

    public DocumentAiAgent(
        IDocumentOcrService ocrService,
        LlmProviderFactory factory,
        IConfiguration configuration,
        ILogger<DocumentAiAgent> logger)
    {
        _ocrService = ocrService;
        _factory = factory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AiAgentResponse> ExecuteAsync(AiAgentRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            return request.Mode switch
            {
                AiAgentRequestMode.Extraction => await HandleExtractionAsync(request, cancellationToken),
                AiAgentRequestMode.Chat => await HandleDocumentChatAsync(request, cancellationToken),
                AiAgentRequestMode.Analysis => await HandleDocumentAnalysisAsync(request, cancellationToken),
                _ => await HandleExtractionAsync(request, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentAiAgent failed");
            return new AiAgentResponse
            {
                AnswerText = "Document processing failed. Please ensure the document is a valid PDF or text file.",
                IsEscalated = true,
                ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private async Task<AiAgentResponse> HandleExtractionAsync(AiAgentRequest request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        var documentPath = request.Context?.GetValueOrDefault("documentPath") as string
            ?? request.Context?.GetValueOrDefault("documentBase64") as string;

        if (string.IsNullOrEmpty(documentPath))
        {
            return new AiAgentResponse
            {
                AnswerText = "No document provided for extraction. Please upload a document.",
                ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }

        Stream documentStream;
        var fileName = request.Context?.GetValueOrDefault("fileName") as string ?? "document.pdf";

        if (documentPath.StartsWith("data:") || documentPath.Length > 500)
        {
            var base64 = documentPath.Contains(',') ? documentPath.Split(',')[1] : documentPath;
            var bytes = Convert.FromBase64String(base64);
            documentStream = new MemoryStream(bytes);
        }
        else if (File.Exists(documentPath))
        {
            documentStream = File.OpenRead(documentPath);
        }
        else
        {
            return new AiAgentResponse
            {
                AnswerText = "Document file not found or invalid format.",
                ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }

        await using (documentStream)
        {
            var ocrResult = await _ocrService.ExtractTextAsync(documentStream, fileName, cancellationToken);

            var structuredData = new
            {
                ocrResult.Classification?.DocumentType,
                ocrResult.PageCount,
                FormFields = ocrResult.FormFields.Select(f => new { f.Label, f.Value, f.Confidence }),
                TextPreview = ocrResult.FullText.Length > 500 ? ocrResult.FullText[..500] + "..." : ocrResult.FullText
            };

            return new AiAgentResponse
            {
                AnswerText = $"Document processed: {fileName}\nType: {ocrResult.Classification?.DocumentType ?? "Unknown"}\nPages: {ocrResult.PageCount}\nFields extracted: {ocrResult.FormFields.Count}\n\n{GenerateSummary(ocrResult)}",
                StructuredData = structuredData,
                ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    private async Task<AiAgentResponse> HandleDocumentChatAsync(AiAgentRequest request, CancellationToken cancellationToken)
    {
        var extractionResponse = await HandleExtractionAsync(request, cancellationToken);
        if (extractionResponse.IsEscalated) return extractionResponse;

        var documentText = extractionResponse.StructuredData is { } data
            ? JsonSerializer.Serialize(data)
            : extractionResponse.AnswerText;

        var universityName = _configuration[$"AI:Tenants:{request.TenantId}:Name"] ?? "the University";
        var llm = _factory.GetLlmProvider(request.TenantId);
        var systemPrompt = SystemPromptTemplate.GenerateDocumentAnalysis(universityName, llm.ModelName);

        var fullPrompt = $"""DOCUMENT CONTENT:\n{documentText}\n\nUSER QUESTION: {request.UserMessage}""";

        var options = new LlmOptions { MaxTokens = 1024, Temperature = 0.2f };
        var response = await llm.CompleteAsync(systemPrompt, fullPrompt, options, cancellationToken);

        return new AiAgentResponse
        {
            AnswerText = response,
            StructuredData = extractionResponse.StructuredData,
            ResponseTimeMs = extractionResponse.ResponseTimeMs + 50
        };
    }

    private async Task<AiAgentResponse> HandleDocumentAnalysisAsync(AiAgentRequest request, CancellationToken cancellationToken)
    {
        return await HandleDocumentChatAsync(request, cancellationToken);
    }

    private string GenerateSummary(OcrResult result)
    {
        var summary = new System.Text.StringBuilder();

        if (result.FormFields.Count > 0)
        {
            summary.AppendLine("Extracted Fields:");
            foreach (var field in result.FormFields)
            {
                summary.AppendLine($"  - {field.Label}: {field.Value}");
            }
        }

        if (result.Classification?.Confidence > 0)
        {
            summary.AppendLine($"\nDocument categorized as: {result.Classification.DocumentType} ({result.Classification.Confidence:P0} confidence)");
        }

        return summary.ToString();
    }
}
