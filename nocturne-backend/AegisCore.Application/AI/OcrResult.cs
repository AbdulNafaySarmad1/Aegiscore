namespace AegisCore.Application.AI;

public class OcrResult
{
    public string FullText { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public List<PageText> Pages { get; set; } = new();
    public List<FormField> FormFields { get; set; } = new();
    public DocumentClassification? Classification { get; set; }
    public double ProcessingTimeMs { get; set; }
}

public class PageText
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class FormField
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? FieldType { get; set; }
    public double Confidence { get; set; }
    public string? NormalizedLabel { get; set; }
}

public class DocumentClassification
{
    public string DocumentType { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> Keywords { get; set; } = new();
}

public enum DocumentType
{
    Unknown,
    Transcript,
    Certificate,
    IdentityCard,
    Application,
    Invoice,
    Report,
    Syllabus,
    Other
}
