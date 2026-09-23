using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// External form integration — a university's surveys (course/teacher evaluations, CLO/PLO
// self-assessments) run in a third-party form tool and arrive here by webhook. The contract is
// generic: a submission is a set of field id → answer pairs; a per-source field mapping says what
// each field means. Nothing here is specific to one form vendor; the vendor adapters only turn a
// vendor's wire format into those pairs (docs/integrations/external-forms.md).

/// <summary>One external form wired to this institution, and the credential its webhook presents.</summary>
[Table("FormIntegrationSources")]
public class FormIntegrationSource : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    /// <summary>Wire format of the incoming webhook: <c>generic</c> (the documented JSON contract) or <c>jotform</c>.</summary>
    [Required, MaxLength(40)] public string Provider { get; set; } = "generic";
    /// <summary>The form's identifier in the external tool; a delivery naming a different form is refused.</summary>
    [MaxLength(200)] public string? ExternalFormId { get; set; }
    /// <summary>SHA-256 of the webhook token. The token itself is shown once, when it is generated.</summary>
    [Required, MaxLength(64)] public string TokenHash { get; set; } = "";
    [MaxLength(8)] public string TokenHint { get; set; } = "";
    /// <summary>The top of the rating scale the form uses (a 1–5 Likert scale is 5).</summary>
    public int ScaleMax { get; set; } = 5;
    /// <summary>For a form about one course only; otherwise a mapped field names the course on each response.</summary>
    public Guid? DefaultCourseId { get; set; }
    [MaxLength(50)] public string? DefaultSemester { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TokenRotatedAt { get; set; }
    public DateTime? LastReceivedAt { get; set; }
}

/// <summary>What one field of an external form means in AegisCore.</summary>
[Table("FormFieldMappings")]
public class FormFieldMapping : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SourceId { get; set; }
    [Required, MaxLength(200)] public string ExternalFieldId { get; set; } = "";
    [MaxLength(1000)] public string QuestionText { get; set; } = "";
    /// <summary>One of <see cref="FormFieldTargets"/>.</summary>
    [Required, MaxLength(30)] public string Target { get; set; } = FormFieldTargets.Unmapped;
    /// <summary>For <c>Clo</c>: the outcome code (e.g. CLO2), resolved against the course each response is about.</summary>
    [MaxLength(50)] public string? CloCode { get; set; }
    /// <summary>For <c>Plo</c>: the programme outcome the answer rates directly.</summary>
    public Guid? ProgramOutcomeId { get; set; }
    /// <summary>For <c>Satisfaction</c>: the heading the item is reported under (e.g. "Instructor").</summary>
    [MaxLength(100)] public string? Category { get; set; }
    public int SortOrder { get; set; }
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public static class FormFieldTargets
{
    public const string Unmapped = "Unmapped";
    public const string Ignore = "Ignore";
    public const string Course = "Course";
    public const string Instructor = "Instructor";
    public const string Semester = "Semester";
    public const string Clo = "Clo";
    public const string Plo = "Plo";
    public const string Satisfaction = "Satisfaction";
    public static readonly string[] All = [Unmapped, Ignore, Course, Instructor, Semester, Clo, Plo, Satisfaction];
    public static bool IsRating(string target) => target is Clo or Plo or Satisfaction;
}

/// <summary>One delivery from an external form, kept so it can be re-applied when the mapping changes.</summary>
[Table("FormIntegrationSubmissions")]
public class FormIntegrationSubmission : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SourceId { get; set; }
    [Required, MaxLength(200)] public string ExternalSubmissionId { get; set; } = "";
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    /// <summary><c>Recorded</c>, <c>Partial</c> (some fields unmapped), or <c>Held</c> (no course could be resolved).</summary>
    [Required, MaxLength(20)] public string Status { get; set; } = "Held";
    public int FieldCount { get; set; }
    public int RecordedCount { get; set; }
    public int UnmappedCount { get; set; }
    [MaxLength(2000)] public string? Problems { get; set; }
    public Guid? CourseId { get; set; }
    [MaxLength(200)] public string? InstructorName { get; set; }
    [MaxLength(50)] public string? Semester { get; set; }
    /// <summary>The answers only (field id → value, with any labels), never the vendor's full envelope.</summary>
    [Required] public string AnswersJson { get; set; } = "{}";
}

/// <summary>
/// One rated answer about a course, an outcome or the teaching, whatever channel it came in on.
/// The indirect-assessment report reads only this table, so it does not know or care whether a
/// response came from a native AegisCore form or an external tool.
/// </summary>
[Table("OutcomeSurveyResponses")]
public class OutcomeSurveyResponse : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    /// <summary><c>native</c>, or <c>external:{provider}</c>.</summary>
    [Required, MaxLength(60)] public string Channel { get; set; } = "native";
    public Guid? SubmissionId { get; set; }
    /// <summary>The AegisCore course evaluation survey the answer was given in, for native responses.</summary>
    public Guid? SurveyId { get; set; }
    /// <summary>Groups the answers of one respondent's response.</summary>
    [Required, MaxLength(200)] public string ResponseKey { get; set; } = "";
    public Guid CourseId { get; set; }
    public Guid? CourseLearningOutcomeId { get; set; }
    public Guid? ProgramOutcomeId { get; set; }
    [MaxLength(100)] public string? Category { get; set; }
    [Required, MaxLength(200)] public string QuestionKey { get; set; } = "";
    [MaxLength(1000)] public string QuestionText { get; set; } = "";
    [Column(TypeName = "decimal(6,2)")] public decimal Score { get; set; }
    public int ScaleMax { get; set; } = 5;
    [MaxLength(200)] public string? InstructorName { get; set; }
    [MaxLength(50)] public string? Semester { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
