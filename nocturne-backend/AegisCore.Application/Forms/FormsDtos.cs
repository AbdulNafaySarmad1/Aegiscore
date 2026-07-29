namespace AegisCore.Application.Forms;

public class FormFieldConfig
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "Text";
    public string Label { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public bool Required { get; set; }
    public string? HelpText { get; set; }
    public string? DefaultValue { get; set; }
    public List<SelectOption>? Options { get; set; }
    public ValidationRule? Validation { get; set; }
    public ConditionalRule? Conditional { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public int Page { get; set; } = 1;
    public int SortOrder { get; set; }
}

public class SelectOption
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ValidationRule
{
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public string? Pattern { get; set; }
    public string? CustomMessage { get; set; }
}

public class ConditionalRule
{
    public string FieldId { get; set; } = string.Empty;
    public string Operator { get; set; } = "Equals";
    public string Value { get; set; } = string.Empty;
}

public class FormDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<FormFieldConfig> Fields { get; set; } = new();
    public int PageCount { get; set; }
    public bool AllowMultipleSubmissions { get; set; }
    public bool AllowDraftSave { get; set; }
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public int SubmissionCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFormRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<FormFieldConfig> Fields { get; set; } = new();
    public bool AllowMultipleSubmissions { get; set; } = true;
    public bool AllowDraftSave { get; set; } = true;
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public string? AccessRoles { get; set; }
    public Guid? WorkflowDefinitionId { get; set; }
}

public class SubmitFormRequest
{
    public Dictionary<string, object?> Values { get; set; } = new();
}

public class SubmissionDto
{
    public Guid Id { get; set; }
    public string FormTitle { get; set; } = string.Empty;
    public string? SubmittedByName { get; set; }
    public string? SubmittedByEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object?> Values { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
