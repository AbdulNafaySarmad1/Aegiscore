using System.Text.Json;
using AegisCore.Application.AI;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.AI;

public class DocumentOcrService : IDocumentOcrService
{
    private readonly ILogger<DocumentOcrService> _logger;

    private static readonly Dictionary<string, string[]> DocumentTypeKeywords = new()
    {
        ["Transcript"] = new[] { "transcript", "grade", "gpa", "semester", "credit", "course" },
        ["Certificate"] = new[] { "certificate", "awarded", "conferred", "degree", "diploma" },
        ["IdentityCard"] = new[] { "student id", "identification", "id card", "registration number" },
        ["Application"] = new[] { "application", "applying", "admission", "enrollment" },
        ["Invoice"] = new[] { "invoice", "bill", "payment", "fee", "amount due", "total" },
        ["Report"] = new[] { "report", "analysis", "findings", "summary" },
        ["Syllabus"] = new[] { "syllabus", "course outline", "learning objectives", "topics", "textbook" }
    };

    public DocumentOcrService(ILogger<DocumentOcrService> logger)
    {
        _logger = logger;
    }

    public async Task<OcrResult> ExtractTextAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new OcrResult();

        try
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            if (extension == ".pdf")
            {
                result = await ExtractFromPdfAsync(documentStream, cancellationToken);
            }
            else if (extension is ".txt" or ".md" or ".csv")
            {
                using var reader = new StreamReader(documentStream);
                var text = await reader.ReadToEndAsync(cancellationToken);
                result.FullText = text;
                result.Pages.Add(new PageText { PageNumber = 1, Text = text, Confidence = 1.0 });
                result.PageCount = 1;
            }
            else
            {
                using var reader = new StreamReader(documentStream);
                var raw = await reader.ReadToEndAsync(cancellationToken);
                result.FullText = raw;
                result.Pages.Add(new PageText { PageNumber = 1, Text = raw, Confidence = 0.5 });
                result.PageCount = 1;
            }

            result.Classification = ClassifyDocument(result.FullText);
            result.FormFields = ExtractFormFieldsFromText(result.FullText);
            result.ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("OCR completed for {FileName}: {Pages} pages, type={DocType}, time={Time}ms",
                fileName, result.PageCount, result.Classification?.DocumentType, result.ProcessingTimeMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR failed for {FileName}", fileName);
            result.FullText = $"OCR extraction failed: {ex.Message}";
            result.ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
        }

        return result;
    }

    public Task<IReadOnlyList<FormField>> ExtractFormFieldsAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default)
    {
        var result = ExtractTextAsync(documentStream, fileName, cancellationToken).Result;
        return Task.FromResult<IReadOnlyList<FormField>>(result.FormFields);
    }

    public async Task<DocumentClassification> ClassifyDocumentAsync(Stream documentStream, string fileName, CancellationToken cancellationToken = default)
    {
        var ocrResult = await ExtractTextAsync(documentStream, fileName, cancellationToken);
        return ocrResult.Classification ?? new DocumentClassification { DocumentType = "Unknown", Confidence = 0 };
    }

    private async Task<OcrResult> ExtractFromPdfAsync(Stream documentStream, CancellationToken cancellationToken)
    {
        var result = new OcrResult();

        try
        {
            using var reader = new StreamReader(documentStream, leaveOpen: true);
            var rawContent = await reader.ReadToEndAsync(cancellationToken);

            var textBuilder = new StringBuilder();
            var inText = false;

            foreach (var line in rawContent.Split('\n'))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("BT"))
                {
                    inText = true;
                    continue;
                }
                if (trimmed.StartsWith("ET"))
                {
                    inText = false;
                    continue;
                }
                if (inText && trimmed.StartsWith("(") && trimmed.EndsWith(")") && trimmed.Contains("Tj"))
                {
                    var text = ExtractPdfTextString(trimmed);
                    if (!string.IsNullOrWhiteSpace(text))
                        textBuilder.Append(text + " ");
                }
            }

            var extracted = textBuilder.ToString().Trim();

            if (string.IsNullOrWhiteSpace(extracted))
            {
                extracted = ExtractReadableStrings(rawContent);
            }

            result.FullText = extracted;
            result.PageCount = 1;
            result.Pages.Add(new PageText { PageNumber = 1, Text = extracted, Confidence = extracted.Length > 0 ? 0.7 : 0.1 });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF extraction failed, falling back to raw text");
            documentStream.Position = 0;
            using var reader = new StreamReader(documentStream);
            var rawText = await reader.ReadToEndAsync(cancellationToken);
            result.FullText = rawText;
            result.PageCount = 1;
            result.Pages.Add(new PageText { PageNumber = 1, Text = rawText, Confidence = 0.3 });
        }

        return result;
    }

    private static string ExtractPdfTextString(string line)
    {
        var start = line.IndexOf('(');
        var end = line.LastIndexOf(')');
        if (start >= 0 && end > start)
        {
            var text = line.Substring(start + 1, end - start - 1);
            return System.Text.RegularExpressions.Regex.Unescape(text);
        }
        return string.Empty;
    }

    private static string ExtractReadableStrings(string rawContent)
    {
        var text = new StringBuilder();
        var matches = System.Text.RegularExpressions.Regex.Matches(rawContent, @"\(([^)]+)\)");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var value = match.Groups[1].Value;
            if (value.Length > 2 && !value.StartsWith("/") && value.Any(char.IsLetter))
            {
                text.Append(value + " ");
            }
        }
        return text.ToString().Trim();
    }

    private DocumentClassification ClassifyDocument(string text)
    {
        var lower = text.ToLowerInvariant();
        var scores = new Dictionary<string, double>();

        foreach (var (docType, keywords) in DocumentTypeKeywords)
        {
            var matches = keywords.Count(k => lower.Contains(k));
            var score = (double)matches / keywords.Length;
            scores[docType] = score;
        }

        var best = scores.OrderByDescending(kv => kv.Value).First();
        var confidence = best.Value;
        var type = confidence > 0.15 ? best.Key : "Unknown";

        var foundKeywords = DocumentTypeKeywords.GetValueOrDefault(type, Array.Empty<string>())
            .Where(k => lower.Contains(k))
            .ToList();

        return new DocumentClassification
        {
            DocumentType = type,
            Confidence = confidence,
            Keywords = foundKeywords
        };
    }

    private List<FormField> ExtractFormFieldsFromText(string text)
    {
        var fields = new List<FormField>();

        var patterns = new Dictionary<string, string>
        {
            ["StudentName"] = @"(?:student\s*name|name\s*of\s*student|candidate\s*name)[:\s]*([^\n,]+)",
            ["DateOfBirth"] = @"(?:date\s*of\s*birth|dob|birth\s*date)[:\s]*([^\n,]+)",
            ["UniversityId"] = @"(?:university\s*id|student\s*id|registration\s*no|roll\s*number)[:\s]*([^\n,]+)",
            ["Degree"] = @"(?:degree|program|course\s*of\s*study)[:\s]*([^\n,]+)",
            ["GPA"] = @"(?:gpa|cgpa|grade\s*point)[:\s]*([^\n,]+)",
            ["Institution"] = @"(?:institution|university|college|school)[:\s]*([^\n,]+)",
            ["GraduationDate"] = @"(?:graduation\s*date|date\s*of\s*graduation|conferred\s*on|awarded\s*on)[:\s]*([^\n,]+)",
            ["Email"] = @"(?:e\-?mail|email\s*address)[:\s]*([^\n,]+)",
            ["Phone"] = @"(?:phone|contact|mobile|tel)[:\s]*([^\n,]+)",
            ["Address"] = @"(?:address|permanent\s*address)[:\s]*([^\n]+)"
        };

        foreach (var (label, pattern) in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                fields.Add(new FormField
                {
                    Label = label,
                    Value = match.Groups[1].Value.Trim(),
                    NormalizedLabel = label,
                    Confidence = 0.8,
                    FieldType = label switch
                    {
                        "GPA" => "number",
                        "DateOfBirth" or "GraduationDate" => "date",
                        "Email" => "email",
                        "Phone" => "phone",
                        _ => "text"
                    }
                });
            }
        }

        return fields;
    }
}
