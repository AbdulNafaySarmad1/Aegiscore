using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("VisitingFacultyProfiles")]
public class VisitingFacultyProfile : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    [MaxLength(50)] public string? Cnic { get; set; }
    [MaxLength(300)] public string? Email { get; set; }
    [MaxLength(50)] public string? Phone { get; set; }
    [Required, MaxLength(50)] public string HighestQualification { get; set; } = string.Empty;
    [MaxLength(200)] public string? DegreeTitle { get; set; }
    [MaxLength(200)] public string? Institution { get; set; }
    public int YearsOfTeachingExperience { get; set; }
    public int? YearsOfIndustryExperience { get; set; }
    [MaxLength(1000)] public string? SpecializationsCsv { get; set; }
    [MaxLength(2000)] public string? ResearchPublicationsCsv { get; set; }
    [MaxLength(500)] public string? ProfessionalCertificationsCsv { get; set; }
    [MaxLength(1000)] public string? AvailabilityWindowsCsv { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? PerCreditHourRate { get; set; }
    [MaxLength(20)] public string HecEligibilityStatus { get; set; } = "pending";
    [MaxLength(20)] public string ProfileStatus { get; set; } = "pending_documents";
    [MaxLength(20)] public string? VerdictForRehire { get; set; }
    public bool DocumentsVerified { get; set; }
    public bool ReferencesChecked { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    public ICollection<FacultyEngagement> Engagements { get; set; } = [];
}

[Table("TeachingSlots")]
public class TeachingSlot : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid SemesterId { get; set; }
    public int CreditHours { get; set; }
    public Guid DepartmentId { get; set; }
    [MaxLength(200)] public string? SpecializationRequired { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal CompensationRate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "open";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("SlotCandidateMatches")]
public class SlotCandidateMatch : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TeachingSlotId { get; set; }
    public Guid FacultyProfileId { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal MatchScore { get; set; }
    public int SpecializationScore { get; set; }
    public int QualificationScore { get; set; }
    public int ExperienceScore { get; set; }
    public int PriorPerformanceScore { get; set; }
    public int CompensationScore { get; set; }
    public bool IsAvailabilityConfirmed { get; set; }
    [MaxLength(500)] public string? MatchRationale { get; set; }
    public int Rank { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FacultyEngagements")]
public class FacultyEngagement : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TeachingSlotId { get; set; }
    public Guid FacultyProfileId { get; set; }
    [Required, MaxLength(50)] public string ContractNumber { get; set; } = string.Empty;
    [Column(TypeName = "decimal(10,2)")] public decimal TotalCompensation { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "draft";
    [MaxLength(20)] public string? ContractStatus { get; set; }
    [MaxLength(500)] public string? IpClause { get; set; }
    public bool AttendanceCompliant { get; set; }
    public bool GradeSubmissionCompliant { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FacultyProfileId))] public VisitingFacultyProfile FacultyProfile { get; set; } = null!;
}

[Table("FacultyPerformanceRatings")]
public class FacultyPerformanceRating : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid EngagementId { get; set; }
    public int AttendanceScore { get; set; }
    public int StudentFeedbackScore { get; set; }
    public int GradeComplianceScore { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal OverallRating { get; set; }
    [MaxLength(500)] public string? DepartmentEndorsement { get; set; }
    [MaxLength(20)] public string RehireRecommendation { get; set; } = "conditional";
    public DateTimeOffset RatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FacultyCompensations")]
public class FacultyCompensation : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid EngagementId { get; set; }
    public int CreditHours { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal PerCreditRate { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal GrossAmount { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal TaxDeduction { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal NetAmount { get; set; }
    [Required, MaxLength(20)] public string PaymentStatus { get; set; } = "pending";
    [MaxLength(50)] public string? VoucherReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
