using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// Course evaluations run inside AegisCore: the registrar (or the QEC office) builds one survey for
// a semester, opens it, and every student answers it once for each course they are enrolled in.
// The CLO questions are not typed in: each student sees the CLOs of the course being evaluated,
// in that course's own words. Answers land in OutcomeSurveyResponse, the store the CLO/PLO report
// reads. Who answered is recorded only to stop a second submission; the answers carry no student.

/// <summary>A course evaluation survey for one semester.</summary>
[Table("OutcomeSurveys")]
public class OutcomeSurvey : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(2000)] public string? Instructions { get; set; }
    [Required, MaxLength(50)] public string Semester { get; set; } = "";
    /// <summary><c>Draft</c> (being written), <c>Open</c> (students can answer) or <c>Closed</c>.</summary>
    [Required, MaxLength(20)] public string Status { get; set; } = "Draft";
    public int ScaleMax { get; set; } = 5;
    /// <summary>Ask each student to rate every CLO of the course being evaluated.</summary>
    public bool AskCourseClos { get; set; } = true;
    [MaxLength(200)] public string CloSection { get; set; } = "Course learning outcomes";
    [MaxLength(500)] public string CloPrompt { get; set; } = "By the end of this course I am able to:";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

/// <summary>One fixed question of a survey. CLO questions are generated per course and are not stored here.</summary>
[Table("OutcomeSurveyQuestions")]
public class OutcomeSurveyQuestion : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SurveyId { get; set; }
    public int SortOrder { get; set; }
    [Required, MaxLength(200)] public string Section { get; set; } = "";
    [Required, MaxLength(1000)] public string Text { get; set; } = "";
    /// <summary><c>Rating</c> (on the survey's scale) or <c>Comment</c> (free text, not scored).</summary>
    [Required, MaxLength(20)] public string Kind { get; set; } = "Rating";
    /// <summary>For a rating: the heading it is reported under (Instructor, Course, Assessment…), unless it rates a PLO.</summary>
    [MaxLength(100)] public string? Category { get; set; }
    /// <summary>A rating that asks about a programme outcome directly.</summary>
    public Guid? ProgramOutcomeId { get; set; }
    public bool Required { get; set; } = true;
}

/// <summary>A course the survey covers. A survey with none covers every course with enrolled students.</summary>
[Table("OutcomeSurveyCourses")]
public class OutcomeSurveyCourse : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SurveyId { get; set; }
    public Guid CourseId { get; set; }
}

/// <summary>That a student has answered a survey for a course — kept apart from the answers themselves.</summary>
[Table("OutcomeSurveyCompletions")]
public class OutcomeSurveyCompletion : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SurveyId { get; set; }
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A free-text answer, kept with the course but not with the student.</summary>
[Table("OutcomeSurveyComments")]
public class OutcomeSurveyComment : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SurveyId { get; set; }
    public Guid CourseId { get; set; }
    public Guid QuestionId { get; set; }
    [Required, MaxLength(4000)] public string Text { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
