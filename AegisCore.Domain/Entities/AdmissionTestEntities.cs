using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

/// <summary>
/// Admin-managed master list of test subjects. Programs pick from this list — no subject
/// name is hardcoded anywhere in the codebase.
/// </summary>
[Table("AdmissionSubjects")]
public class AdmissionSubject : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Per intake + program test blueprint: pass mark and time limit.</summary>
[Table("ProgramTestProfiles")]
public class ProgramTestProfile : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public Guid IntakeId { get; set; }
    public Guid ProgramId { get; set; }

    public int PassMark { get; set; }
    public int TimeLimitMinutes { get; set; } = 60;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProgramTestSubject> Subjects { get; set; } = [];
}

/// <summary>A subject slot within a program's test profile: how many questions and marks each.</summary>
[Table("ProgramTestSubjects")]
public class ProgramTestSubject : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public Guid TestProfileId { get; set; }
    public Guid SubjectId { get; set; }

    public int QuestionCount { get; set; }
    public int MarksPerQuestion { get; set; } = 1;

    public ProgramTestProfile TestProfile { get; set; } = null!;
}

/// <summary>An uploaded syllabus PDF for a subject, plus its extracted text.</summary>
[Table("SyllabusDocuments")]
public class SyllabusDocument : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public Guid SubjectId { get; set; }

    [Required, MaxLength(600)] public string StoragePath { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string OriginalFileName { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    public SyllabusOcrStatus OcrStatus { get; set; } = SyllabusOcrStatus.Pending;
    [Column(TypeName = "text")] public string? ExtractedText { get; set; }
    [MaxLength(30)] public string? OcrMethod { get; set; }

    public Guid CreatedBy { get; set; }
}

/// <summary>
/// Editable LLM system prompt for question generation. Stored in the DB, not hardcoded;
/// admin edits it at /admin/admissions/syllabus.
/// </summary>
[Table("QuestionGenerationPrompts")]
public class QuestionGenerationPrompt : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    [Required, MaxLength(120)] public string Name { get; set; } = "Default";
    [Required, Column(TypeName = "text")] public string SystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// A generated multiple-choice question. Only <see cref="AdmissionQuestionStatus.Approved"/>
/// questions are used to build live papers.
/// </summary>
[Table("AdmissionQuestions")]
public class AdmissionQuestion : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public Guid SubjectId { get; set; }

    [Required, MaxLength(2000)] public string QuestionText { get; set; } = string.Empty;
    [Required, MaxLength(600)] public string OptionA { get; set; } = string.Empty;
    [Required, MaxLength(600)] public string OptionB { get; set; } = string.Empty;
    [Required, MaxLength(600)] public string OptionC { get; set; } = string.Empty;
    [Required, MaxLength(600)] public string OptionD { get; set; } = string.Empty;

    /// <summary>"A", "B", "C" or "D".</summary>
    [Required, MaxLength(1)] public string CorrectOption { get; set; } = "A";

    [MaxLength(30)] public string DifficultyLevel { get; set; } = "medium";
    [Column(TypeName = "text")] public string? SourceChunk { get; set; }

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public AdmissionQuestionStatus Status { get; set; } = AdmissionQuestionStatus.Draft;
}

/// <summary>
/// A per-applicant randomized paper. The question set and option order differ per applicant;
/// <see cref="PaperHash"/> is a SHA-256 of the ordered question ids for audit.
/// </summary>
[Table("ApplicantTests")]
public class ApplicantTest : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public Guid ApplicantId { get; set; }
    public Guid TestProfileId { get; set; }

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }

    [Required, MaxLength(64)] public string PaperHash { get; set; } = string.Empty;
    /// <summary>JSON: [{ questionId, order: ["B","A","D","C"] }] — the shuffled option order per question.</summary>
    [Required, Column(TypeName = "text")] public string PaperJson { get; set; } = "[]";
    /// <summary>JSON: { questionId: "A" } — the applicant's chosen letters (post-shuffle mapping resolved on submit).</summary>
    [Column(TypeName = "text")] public string ResponsesJson { get; set; } = "{}";

    public int? Score { get; set; }
    public bool? Passed { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? ProctorId { get; set; }
    [MaxLength(500)] public string? ProctorNote { get; set; }

    public ApplicantTestStatus Status { get; set; } = ApplicantTestStatus.Scheduled;
}
