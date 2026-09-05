using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AegisCore.Application.Onboarding;
using AegisCore.Domain.Entities;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;

namespace AegisCore.Infrastructure.Onboarding;

public sealed record DocumentTemplateResult(
    Guid Id,
    string Name,
    string PreviewHtml,
    IReadOnlyList<string> ExtractedPalette,
    IReadOnlyList<string> ExtractedFonts,
    string? LogoUrl);

public sealed record DocumentTemplateWriteRequest(string Name, string RawHtml, string? LogoUrl = null);

public sealed class DocumentTemplateService(AegisDbContext db, WizardStateService states)
{
    private const int MaximumBytes = 10 * 1024 * 1024;
    private static readonly Regex ColorRegex = new("(?<![A-Za-z0-9])#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?\\b|rgba?\\([^)]{1,80}\\)", RegexOptions.Compiled);
    private static readonly Regex FontRegex = new("font-family\\s*:\\s*([^;}]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UnsafeElementRegex = new("<(script|iframe|object|embed|base|form)\\b[^>]*>.*?</\\1\\s*>|<(script|iframe|object|embed|base|form)\\b[^>]*/?>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex EventAttributeRegex = new("\\s+on[a-z]+\\s*=\\s*(?:\"[^\"]*\"|'[^']*'|[^\\s>]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UnsafeUrlRegex = new("(?<attr>href|src)\\s*=\\s*(?<quote>['\"]?)\\s*(?:javascript|data\\s*:\\s*text/html)[^'\" >]*\\k<quote>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<DocumentTemplateResult> UploadAsync(string token, string fileName, Stream content, string? name, string? logoUrl, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".html" or ".htm" or ".pdf" or ".docx"))
            throw new ArgumentException("Document template must be an HTML, PDF, or DOCX file.");
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        if (buffer.Length is 0 or > MaximumBytes)
            throw new ArgumentException("Document template must be between 1 byte and 10 MiB.");
        var bytes = buffer.ToArray();

        ParsedTemplate parsed = extension switch
        {
            ".html" or ".htm" => ParseHtml(Encoding.UTF8.GetString(bytes)),
            ".pdf" => ParsePdf(bytes),
            ".docx" => ParseDocx(bytes),
            _ => throw new InvalidOperationException("Unsupported document template format.")
        };
        return await SaveAsync(token, name ?? Path.GetFileNameWithoutExtension(fileName), parsed, logoUrl, ct);
    }

    public Task<DocumentTemplateResult> WriteAsync(string token, DocumentTemplateWriteRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
            throw new ArgumentException("Template name is required and must be 200 characters or fewer.");
        if (string.IsNullOrWhiteSpace(request.RawHtml) || Encoding.UTF8.GetByteCount(request.RawHtml) > MaximumBytes)
            throw new ArgumentException("Template HTML is required and must not exceed 10 MiB.");
        return SaveAsync(token, request.Name.Trim(), ParseHtml(request.RawHtml), request.LogoUrl, ct);
    }

    public async Task<DocumentTemplateResult> PreviewAsync(string token, CancellationToken ct = default)
    {
        var state = await states.InitializeTenantAtStepOneAsync(token, ct);
        if (state.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before previewing a document template.");
        var template = await db.DocumentTemplates.IgnoreQueryFilters()
            .Where(item => item.TenantId == state.TenantId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (template is null) return DefaultResult();
        return ToResult(template);
    }

    public static DocumentTemplateResult DefaultResult() => new(
        Guid.Empty,
        "AegisCore default",
        RenderSample("<header><h1>{{university}}</h1></header><main><h2>Academic transcript</h2><p>{{student}}</p>{{records}}</main>", null),
        ["#102a43", "#2f80ed", "#ffffff"],
        ["Inter", "system-ui"],
        null);

    private async Task<DocumentTemplateResult> SaveAsync(string token, string name, ParsedTemplate parsed, string? logoUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            throw new ArgumentException("Template name is required and must be 200 characters or fewer.");
        var state = await states.InitializeTenantAtStepOneAsync(token, ct);
        if (state.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before saving a document template.");
        var entity = new DocumentTemplate
        {
            Id = Guid.NewGuid(), TenantId = state.TenantId, Name = name.Trim(), RawHtml = parsed.Html,
            ExtractedPalette = JsonSerializer.Serialize(parsed.Colors), ExtractedFonts = JsonSerializer.Serialize(parsed.Fonts),
            LogoUrl = NormalizeLogoUrl(logoUrl), CreatedAt = DateTime.UtcNow
        };
        db.DocumentTemplates.Add(entity);
        await db.SaveChangesAsync(ct);
        return ToResult(entity);
    }

    private static DocumentTemplateResult ToResult(DocumentTemplate template)
    {
        var colors = JsonSerializer.Deserialize<string[]>(template.ExtractedPalette) ?? [];
        var fonts = JsonSerializer.Deserialize<string[]>(template.ExtractedFonts) ?? [];
        return new(template.Id, template.Name, RenderSample(template.RawHtml, template.LogoUrl), colors, fonts, template.LogoUrl);
    }

    private static ParsedTemplate ParseHtml(string source)
    {
        var safe = SanitizeHtml(source);
        var colors = ColorRegex.Matches(safe).Select(match => NormalizeColor(match.Value)).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToArray();
        var fonts = FontRegex.Matches(safe).SelectMany(match => match.Groups[1].Value.Split(','))
            .Select(font => font.Trim().Trim('\'', '"')).Where(font => font.Length is > 0 and <= 100)
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToArray();
        return new(safe, colors.Length == 0 ? ["#102a43", "#2f80ed"] : colors, fonts.Length == 0 ? ["system-ui"] : fonts);
    }

    private static ParsedTemplate ParsePdf(byte[] bytes)
    {
        var text = new StringBuilder();
        using (var document = PdfDocument.Open(bytes))
            foreach (var page in document.GetPages()) text.AppendLine(page.Text);
        if (text.Length == 0) throw new ArgumentException("The PDF contains no extractable text. Use an HTML or DOCX template for scanned documents.");

        var ascii = Encoding.Latin1.GetString(bytes);
        var colors = Regex.Matches(ascii, @"(?<r>0(?:\.\d+)?|1(?:\.0+)?)\s+(?<g>0(?:\.\d+)?|1(?:\.0+)?)\s+(?<b>0(?:\.\d+)?|1(?:\.0+)?)\s+(?:rg|RG)\b")
            .Select(match => $"#{ToByte(match.Groups["r"].Value):X2}{ToByte(match.Groups["g"].Value):X2}{ToByte(match.Groups["b"].Value):X2}")
            .Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToArray();
        var html = $"<main class=\"imported-pdf\"><h1>Imported document template</h1><div>{WebUtility.HtmlEncode(text.ToString()).Replace("\r\n", "<br>").Replace("\n", "<br>")}</div></main>";
        return new(html, colors.Length == 0 ? ["#102a43", "#ffffff"] : colors, ["PDF embedded font"]);
    }

    private static ParsedTemplate ParseDocx(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body ?? throw new ArgumentException("The DOCX document has no body.");
        var paragraphs = body.Descendants<Paragraph>().Select(paragraph => $"<p>{WebUtility.HtmlEncode(paragraph.InnerText)}</p>");
        var fonts = document.MainDocumentPart?.StyleDefinitionsPart?.Styles?.Descendants<RunFonts>()
            .SelectMany(font => new[] { font.Ascii?.Value, font.HighAnsi?.Value })
            .Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToArray() ?? [];
        var colors = document.MainDocumentPart?.StyleDefinitionsPart?.Styles?.Descendants<Color>()
            .Select(color => color.Val?.Value).Where(value => value is { Length: 6 } && value.All(Uri.IsHexDigit))
            .Select(value => "#" + value).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToArray() ?? [];

        return new("<main class=\"imported-docx\">" + string.Join("", paragraphs) + "</main>",
            colors.Length == 0 ? ExtractThemeColors(bytes) : colors,
            fonts.Length == 0 ? ["Calibri"] : fonts);
    }

    private static string[] ExtractThemeColors(byte[] bytes)
    {
        using var archive = new ZipArchive(new MemoryStream(bytes, false), ZipArchiveMode.Read);
        var entry = archive.GetEntry("word/theme/theme1.xml");
        if (entry is null) return ["#102a43", "#2f80ed"];
        using var reader = new StreamReader(entry.Open());
        var xml = XDocument.Parse(reader.ReadToEnd());
        var colors = xml.Descendants().Attributes().Where(attribute => attribute.Name.LocalName is "val" or "lastClr")
            .Select(attribute => attribute.Value).Where(value => value.Length == 6 && value.All(Uri.IsHexDigit))
            .Select(value => "#" + value).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToArray();
        return colors.Length == 0 ? ["#102a43", "#2f80ed"] : colors;
    }

    private static string SanitizeHtml(string html)
    {
        var safe = UnsafeElementRegex.Replace(html, string.Empty);
        safe = EventAttributeRegex.Replace(safe, string.Empty);
        return UnsafeUrlRegex.Replace(safe, match => match.Groups["attr"].Value + "=\"#\"");
    }

    private static string RenderSample(string html, string? logoUrl)
    {
        var records = "<table><thead><tr><th>Course</th><th>Grade</th></tr></thead><tbody><tr><td>Systems Design</td><td>A</td></tr><tr><td>Data Ethics</td><td>A-</td></tr></tbody></table>";
        var logo = string.IsNullOrWhiteSpace(logoUrl) ? string.Empty : $"<img class=\"template-logo\" src=\"{WebUtility.HtmlEncode(logoUrl)}\" alt=\"University logo\">";
        return logo + html.Replace("{{university}}", "Example University", StringComparison.Ordinal)
            .Replace("{{student}}", "Student: Sample Learner", StringComparison.Ordinal)
            .Replace("{{records}}", records, StringComparison.Ordinal);
    }

    private static string? NormalizeLogoUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Logo URL must use HTTPS.");
        return uri.ToString();
    }

    private static int ToByte(string component) => (int)Math.Round(decimal.Parse(component, CultureInfo.InvariantCulture) * 255m);
    private static string NormalizeColor(string value) => value.ToLowerInvariant();
    private sealed record ParsedTemplate(string Html, string[] Colors, string[] Fonts);
}
