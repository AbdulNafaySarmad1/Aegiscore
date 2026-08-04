using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("AssessmentSchemes")]
public class AssessmentScheme
{
    [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid ExamControllerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<AssessmentComponent> Components { get; set; } = [];
}

[Table("AssessmentComponents")]
public class AssessmentComponent
{
    [Key] public Guid Id { get; set; }
    public Guid SchemeId { get; set; }
    [Required, MaxLength(50)] public string ComponentType { get; set; } = string.Empty;
    public int WeightPercentage { get; set; }
    public int TotalMarks { get; set; }
    public int? PassingMarks { get; set; }
    public bool IsMandatory { get; set; }
    public int OrderIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public AssessmentScheme Scheme { get; set; } = null!;
}

[Table("CourseMarks")]
public class CourseMark
{
    [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }
    public Guid AssessmentComponentId { get; set; }
    public int ObtainedMarks { get; set; }
    public DateTimeOffset EntryDate { get; set; } = DateTimeOffset.UtcNow;
    public Guid EnteredBy { get; set; }
    public AssessmentComponent AssessmentComponent { get; set; } = null!;
}

[Table("MarkModerations")]
public class MarkModeration
{
    [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid SemesterId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid SubmittedBy { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewComments { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("GradeScales")]
public class GradeScale
{
    [Key] public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<GradeBand> Grades { get; set; } = [];
}

[Table("GradeBands")]
public class GradeBand
{
    [Key] public Guid Id { get; set; }
    public Guid GradeScaleId { get; set; }
    [Required, MaxLength(2)] public string LetterGrade { get; set; } = string.Empty;
    [Column(TypeName = "decimal(3,2)")] public decimal GpaPoints { get; set; }
    public int PercentageFrom { get; set; }
    public int PercentageTo { get; set; }
    [MaxLength(200)] public string? Description { get; set; }
    public GradeScale GradeScale { get; set; } = null!;
}

[Table("CourseGrades")]
public class CourseGrade
{
    [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid StudentId { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal TotalMarksObtained { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal PercentageObtained { get; set; }
    [MaxLength(2)] public string? LetterGrade { get; set; }
    [Column(TypeName = "decimal(3,2)")] public decimal GradePoints { get; set; }
    public bool IsPassing { get; set; }
    [Required, MaxLength(20)] public string ResultStatus { get; set; } = "pending";
    public DateTimeOffset? CalculatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}

[Table("ExamRegistrations")]
public class ExamRegistration
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public DateOnly ExamDate { get; set; }
    public TimeOnly ExamTime { get; set; }
    [MaxLength(100)] public string? ExamHall { get; set; }
    public int? SeatNumber { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal AttendancePercentage { get; set; }
    public bool IsAttendanceEligible { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("Transcripts")]
public class Transcript
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    [Required] public string PdfFilePath { get; set; } = string.Empty;
    [MaxLength(64)] public string? VerificationHash { get; set; }
    public bool IsOfficial { get; set; }
    public Guid? RequestedBy { get; set; }
}
