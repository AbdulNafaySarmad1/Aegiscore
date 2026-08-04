using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("SystemConfigurations")]
public class SystemConfiguration
{
    [Key] public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    [Range(1, 72)] public int ReplicationLagHours { get; set; } = 12;
    [Required] public string PrimaryDbConnectionString { get; set; } = string.Empty;
    public string? ReadReplica1ConnectionString { get; set; }
    public string? ReadReplica2ConnectionString { get; set; }
    public string? WriteReplicaConnectionString { get; set; }
    [Required] public string RedisConnectionString { get; set; } = string.Empty;
    [Range(1, 1440)] public int RedisCacheTtlMinutes { get; set; } = 15;
    [MaxLength(200)] public string? SubdomainName { get; set; }
    public bool CustomDnsEnabled { get; set; }
    public string? DnsNameserver1 { get; set; }
    public string? DnsNameserver2 { get; set; }
    public int RegularClassCapacity { get; set; } = 60;
    public int ExamClassCapacity { get; set; } = 80;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("GradeOverrideRequests")]
public class GradeOverrideRequest
{
    [Key] public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid SemesterId { get; set; }
    [MaxLength(2)] public string? OriginalGrade { get; set; }
    public int OriginalMarks { get; set; }
    [Column(TypeName = "decimal(3,2)")] public decimal OriginalGpa { get; set; }
    [MaxLength(2)] public string? OverrideGrade { get; set; }
    public int OverrideMarks { get; set; }
    [Column(TypeName = "decimal(3,2)")] public decimal OverrideGpa { get; set; }
    [Required, MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Justification { get; set; }
    public Guid RequestedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    [Required, MaxLength(20)] public string ApprovalStatus { get; set; } = "pending";
    [Required, MaxLength(20)] public string ApprovalThreshold { get; set; } = "hod";
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ApprovedAt { get; set; }
}

[Table("SectionOverrideRequests")]
public class SectionOverrideRequest
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid FromSectionId { get; set; }
    public Guid ToSectionId { get; set; }
    public int CurrentRegularCount { get; set; }
    public int CurrentExamCount { get; set; }
    public bool CapacityAllowed { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal FeeAdjustment { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid RequestedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("AnonymousFeedbackSubmissions")]
public class AnonymousFeedbackSubmission
{
    [Key] public Guid Id { get; set; }
    public Guid UniversityId { get; set; }
    public Guid CourseId { get; set; }
    public Guid FacultyId { get; set; }
    public int Rating { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal SentimentScore { get; set; }
    [Required] public string EncryptedPayload { get; set; } = string.Empty;
    [MaxLength(300)] public string ThemesCsv { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("CourseReEnrollments")]
public class CourseReEnrollment
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid SourceSemesterId { get; set; }
    public Guid TargetSemesterId { get; set; }
    public bool FeeWaived { get; set; } = true;
    [Required, MaxLength(20)] public string Status { get; set; } = "scheduled";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
