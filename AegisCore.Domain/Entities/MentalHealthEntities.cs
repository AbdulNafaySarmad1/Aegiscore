using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("MentalHealthTickets")]
public class MentalHealthTicket : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(50)] public string AnonymousToken { get; set; } = string.Empty;
    [MaxLength(20)] public string Severity { get; set; } = "routine";
    [MaxLength(50)] public string ConcernType { get; set; } = "academic_stress";
    [MaxLength(4000)] public string? ConcernDescription { get; set; }
    [MaxLength(10)] public string? PreferredLanguage { get; set; } = "en";
    [Required, MaxLength(20)] public string Status { get; set; } = "open";
    public Guid? AssignedCounselorId { get; set; }
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? FirstResponseAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    [MaxLength(20)] public string? ResolutionType { get; set; }
    public bool CrisisProtocolsTriggered { get; set; }
    public bool MandatoryReportTriggered { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("CounselorProfiles")]
public class CounselorProfile : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid UserId { get; set; }
    [MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [MaxLength(500)] public string? SpecializationsCsv { get; set; }
    [MaxLength(20)] public string LanguagesCsv { get; set; } = "en";
    public bool IsOnCall { get; set; }
    public int CurrentCaseload { get; set; }
    public int MaxCaseload { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("CounselingCaseNotes")]
public class CounselingCaseNote : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid CounselorId { get; set; }
    [MaxLength(3000)] public string? NoteContent { get; set; }
    public bool IsPrivateToCounselor { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("MandatoryReportLogs")]
public class MandatoryReportLog : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    [Required, MaxLength(100)] public string TriggerCriterion { get; set; } = string.Empty;
    [MaxLength(1000)] public string? ActionTaken { get; set; }
    [MaxLength(100)] public string? ReportedTo { get; set; }
    public DateTimeOffset ReportedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("WellnessCheckIns")]
public class WellnessCheckIn : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [MaxLength(50)] public string AnonymousToken { get; set; } = string.Empty;
    public int WellbeingScore { get; set; }
    public DateTimeOffset CheckInDate { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
