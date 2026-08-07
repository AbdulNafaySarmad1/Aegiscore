using AegisCore.Application.AI;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.AI;

public sealed class AzureDocumentIntelligenceService : IDocumentOcrService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AzureDocumentIntelligenceService> _logger;
    private readonly DocumentOcrService _fallback;

    public AzureDocumentIntelligenceService(IConfiguration config, ILogger<AzureDocumentIntelligenceService> logger, ILogger<DocumentOcrService> fallbackLogger)
    {
        _config = config;
        _logger = logger;
        _fallback = new DocumentOcrService(fallbackLogger);
    }

    public async Task<OcrResult> ExtractTextAsync(Stream documentStream, string fileName, CancellationToken ct = default)
    {
        var endpoint = _config["Azure:DocumentIntelligenceEndpoint"];
        var apiKey = _config["Azure:DocumentIntelligenceApiKey"];

        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.StartsWith("${", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("${", StringComparison.Ordinal))
        {
            _logger.LogInformation("Azure Document Intelligence not configured. Using local OCR for {FileName}.", fileName);
            return await _fallback.ExtractTextAsync(documentStream, fileName, ct);
        }

        try
        {
            var credential = new AzureKeyCredential(apiKey);
            var client = new DocumentIntelligenceClient(new Uri(endpoint), credential);

            documentStream.Position = 0;
            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-read",
                BinaryData.FromStream(documentStream),
                cancellationToken: ct);

            var result = new OcrResult();
            foreach (var page in operation.Value.Pages)
            {
                var lines = page.Lines?.Select(l => l.Content) ?? [];
                result.Pages.Add(new PageText
                {
                    PageNumber = page.PageNumber,
                    Text = string.Join(" ", lines),
                    Confidence = 0.95
                });
            }
            result.FullText = string.Join("\n", result.Pages.Select(p => p.Text));
            result.PageCount = result.Pages.Count;

            _logger.LogInformation("Azure Document Intelligence processed {FileName}: {PageCount} pages.", fileName, result.PageCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Document Intelligence call failed for {FileName}. Falling back to local OCR.", fileName);
            return await _fallback.ExtractTextAsync(documentStream, fileName, ct);
        }
    }

    public async Task<IReadOnlyList<FormField>> ExtractFormFieldsAsync(Stream documentStream, string fileName, CancellationToken ct = default)
    {
        var endpoint = _config["Azure:DocumentIntelligenceEndpoint"];
        var apiKey = _config["Azure:DocumentIntelligenceApiKey"];

        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.StartsWith("${", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("${", StringComparison.Ordinal))
        {
            _logger.LogInformation("Azure Document Intelligence not configured. Using local form extraction for {FileName}.", fileName);
            return await _fallback.ExtractFormFieldsAsync(documentStream, fileName, ct);
        }

        try
        {
            var credential = new AzureKeyCredential(apiKey);
            var client = new DocumentIntelligenceClient(new Uri(endpoint), credential);

            documentStream.Position = 0;
            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-document",
                BinaryData.FromStream(documentStream),
                cancellationToken: ct);

            var fields = new List<FormField>();
            foreach (var doc in operation.Value.Documents)
            {
                foreach (var kvp in doc.Fields)
                {
                    var key = kvp.Key;
                    var value = kvp.Value;
                    var str = value?.ValueString ?? value?.Content;
                    if (str is { Length: > 0 })
                    {
                        fields.Add(new FormField
                        {
                            Label = key,
                            Value = str,
                            NormalizedLabel = key.ToLowerInvariant().Replace(" ", ""),
                            Confidence = value?.Confidence ?? 0.95
                        });
                    }
                }
            }

            _logger.LogInformation("Azure Document Intelligence extracted {FieldCount} fields from {FileName}.", fields.Count, fileName);
            return fields;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Document Intelligence form extraction failed for {FileName}.", fileName);
            return await _fallback.ExtractFormFieldsAsync(documentStream, fileName, ct);
        }
    }

    public async Task<DocumentClassification> ClassifyDocumentAsync(Stream documentStream, string fileName, CancellationToken ct = default)
    {
        var ocrResult = await ExtractTextAsync(documentStream, fileName, ct);
        return ocrResult.Classification ?? new DocumentClassification { DocumentType = "Unknown", Confidence = 0 };
    }
}
