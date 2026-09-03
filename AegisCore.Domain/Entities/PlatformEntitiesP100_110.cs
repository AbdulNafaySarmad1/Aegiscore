using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

// P100 Customer Portal
[Table("SupportTickets")] public class SupportTicket : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Subject { get; set; } = ""; [MaxLength(5000)] public string? Description { get; set; }
    [MaxLength(50)] public string Priority { get; set; } = "Medium"; [MaxLength(50)] public string Status { get; set; } = "Open";
    public Guid CustomerId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; } public Guid TenantId { get; set; } }

[Table("ReleaseDownloads")] public class ReleaseDownload : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(100)] public string Version { get; set; } = ""; public UpdateChannel Channel { get; set; }
    [MaxLength(500)] public string? DownloadUrl { get; set; } [MaxLength(100)] public string? Checksum { get; set; }
    [MaxLength(5000)] public string? ReleaseNotes { get; set; } public DateTime ReleasedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P101 Update Service
[Table("UpdateDeployments")] public class UpdateDeployment : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(100)] public string FromVersion { get; set; } = ""; [MaxLength(100)] public string ToVersion { get; set; } = "";
    public UpdateStatus Status { get; set; } = UpdateStatus.Available; public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; } [MaxLength(5000)] public string? Log { get; set; }
    public bool IsRolledBack { get; set; } public Guid TenantId { get; set; } }

// P102 Official Marketplace
[Table("OfficialModules")] public class OfficialModule : ITenantScoped { [Key] public Guid Id { get; set; }
    [Required][MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(100)] public string? Category { get; set; }
    [MaxLength(50)] public string Version { get; set; } = ""; [MaxLength(2000)] public string? Description { get; set; }
    public decimal Price { get; set; } public bool IsPublished { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P103 Fee Defaulter
[Table("FeeDefaulterRecords")] public class FeeDefaulterRecord : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal AmountDue { get; set; }
    public DefaulterStatus Status { get; set; } = DefaulterStatus.Pending;
    public FeeDefaulterLevel Level { get; set; } = FeeDefaulterLevel.Level1_Reminder;
    public int NotificationsSent { get; set; } public DateTime? LastNotifiedAt { get; set; }
    public DateTime? RTPAttemptedAt { get; set; } public bool RTPAttempted { get; set; }
    public DateTime DueDate { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P104 Academic Misconduct
[Table("MisconductCases")] public class MisconductCase : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(300)] public string Title { get; set; } = "";
    [MaxLength(5000)] public string? Description { get; set; } public MisconductSeverity Severity { get; set; }
    public MisconductStatus Status { get; set; } = MisconductStatus.Reported;
    public Guid ReportedByUserId { get; set; } public Guid? AssignedToUserId { get; set; }
    public DateTime? HearingDate { get; set; } public SanctionType? Sanction { get; set; }
    [MaxLength(2000)] public string? Resolution { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; } public Guid TenantId { get; set; } }

[Table("MisconductEvidence")] public class MisconductEvidence : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CaseId { get; set; } [MaxLength(500)] public string? FileUrl { get; set; }
    [MaxLength(1000)] public string? Description { get; set; } public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P105 Faculty Workload
[Table("FacultyWorkloads")] public class FacultyWorkload : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid FacultyId { get; set; } public Guid CourseId { get; set; }
    public WorkloadUnitType UnitType { get; set; } = WorkloadUnitType.Lecture;
    public int HoursPerWeek { get; set; } public int StudentCount { get; set; }
    [MaxLength(200)] public string? Schedule { get; set; } public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; } public Guid TenantId { get; set; } }

// P106 Degree Verification
[Table("DegreeVerifications")] public class DegreeVerification : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(100)] public string VerificationToken { get; set; } = "";
    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
    [MaxLength(300)] public string? RequestedBy { get; set; } [MaxLength(100)] public string? EmployerName { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow; public DateTime? VerifiedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } public Guid TenantId { get; set; } }

// P107 Semester Registration
[Table("RegistrationWindows")] public class RegistrationWindow : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(50)] public string? Semester { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Open;
    public DateTime OpensAt { get; set; } public DateTime ClosesAt { get; set; }
    public int MaxCoursesPerStudent { get; set; } = 6; public bool RequiresAdvisorApproval { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("RegistrationEnrollments")] public class RegistrationEnrollment : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid WindowId { get; set; } public Guid StudentId { get; set; } public Guid CourseId { get; set; }
    public RegistrationStatus Status { get; set; } = RegistrationStatus.InProgress;
    public bool IsWaitlisted { get; set; } public int WaitlistPosition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P108 Curriculum Governance
[Table("CurriculumProposals")] public class CurriculumProposal : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Title { get; set; } = ""; [MaxLength(5000)] public string? Description { get; set; }
    public CurriculumApprovalStatus Status { get; set; } = CurriculumApprovalStatus.Draft;
    public Guid ProposedByUserId { get; set; } [Column(TypeName = "jsonb")] public string ChangesJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? ApprovedAt { get; set; } public Guid TenantId { get; set; } }

// P109 Zero Trust
[Table("TunnelConfigurations")] public class TunnelConfiguration : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(500)] public string? TunnelUrl { get; set; }
    public TunnelStatus Status { get; set; } = TunnelStatus.Disconnected;
    [MaxLength(100)] public string? TokenHash { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("AccessAuditLogs")] public class AccessAuditLog : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(100)] public string? SourceIp { get; set; } [MaxLength(500)] public string? RequestPath { get; set; }
    public int StatusCode { get; set; } public bool IsBlocked { get; set; } public DateTime RecordedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P110 Hardcoding Audit
[Table("HardcodingFindings")] public class HardcodingFinding : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(500)] public string FilePath { get; set; } = ""; public int LineNumber { get; set; }
    [MaxLength(1000)] public string? CodeSnippet { get; set; } public HardcodingType Type { get; set; }
    public HardcodingSeverity Severity { get; set; } [MaxLength(500)] public string? Recommendation { get; set; }
    public bool IsResolved { get; set; } public DateTime FoundAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }
