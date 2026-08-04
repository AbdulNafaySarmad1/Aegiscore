using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("BoardMeetings")]
public class BoardMeeting : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "scheduled";
    [MaxLength(500)] public string? ExecutiveSummary { get; set; }
    [MaxLength(500)] public string? PackPdfUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("BoardDecisions")]
public class BoardDecision : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid? MeetingId { get; set; }
    [Required, MaxLength(300)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Background { get; set; }
    [MaxLength(2000)] public string? OptionsCsv { get; set; }
    [MaxLength(1000)] public string? Recommendation { get; set; }
    [MaxLength(200)] public string? ImpactAssessment { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("StrategicGoals")]
public class StrategicGoal : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(300)] public string GoalTitle { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal TargetValue { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal CurrentValue { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "off_track";
    public DateTimeOffset TargetDate { get; set; }
    public DateTimeOffset? LastUpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("InstitutionBenchmarks")]
public class InstitutionBenchmark : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(100)] public string MetricName { get; set; } = string.Empty;
    [Column(TypeName = "decimal(10,2)")] public decimal InstitutionValue { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal NationalAverage { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? PeerInstitutionValue { get; set; }
    [MaxLength(20)] public string PerformanceVsNational { get; set; } = "at_average";
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("BoardAlerts")]
public class BoardAlert : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [MaxLength(20)] public string Severity { get; set; } = "info";
    [MaxLength(100)] public string Category { get; set; } = string.Empty;
    [MaxLength(500)] public string? Message { get; set; }
    [MaxLength(500)] public string? RecommendedAction { get; set; }
    [MaxLength(100)] public string? ResponsibleOfficer { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("BoardResolutions")]
public class BoardResolution : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    [Required, MaxLength(20)] public string ResolutionNumber { get; set; } = string.Empty;
    [MaxLength(500)] public string? Content { get; set; }
    [MaxLength(20)] public string ImplementationStatus { get; set; } = "not_started";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
