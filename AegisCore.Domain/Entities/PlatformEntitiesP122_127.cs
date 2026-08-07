using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// P122 Student Achievement Gamification
[Table("AchievementBadges")] public class AchievementBadge { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(500)] public string? Description { get; set; }
    public BadgeCategory Category { get; set; } public AwardTier Tier { get; set; } = AwardTier.Bronze;
    [MaxLength(500)] public string? ImageUrl { get; set; } [Column(TypeName = "jsonb")] public string CriteriaJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("StudentBadges")] public class StudentBadge { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid BadgeId { get; set; }
    [MaxLength(300)] public string? AchievementDetail { get; set; } [MaxLength(100)] public string? VerificationHash { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow; public bool NotifiedParent { get; set; } public Guid TenantId { get; set; } }




// P123 Mental Health Ticketing

[Table("MandatoryReports")] public class MandatoryReport { [Key] public Guid Id { get; set; }
    public Guid TicketId { get; set; } public MandatoryReportTrigger Trigger { get; set; }
    [MaxLength(2000)] public string? ReportContent { get; set; } [MaxLength(200)] public string? AuthorityNotified { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow; public DateTime? AcknowledgedAt { get; set; } public Guid TenantId { get; set; } }



// P124 Foreign Student Visa & MORA
[Table("ForeignStudentRegistries")] public class ForeignStudentRegistry { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(100)] public string Nationality { get; set; } = "";
    [MaxLength(50)] public string? PassportNumber { get; set; } public DateTime? PassportExpiry { get; set; }
    public VisaType VisaType { get; set; } [MaxLength(50)] public string? VisaNumber { get; set; }
    public DateTime? VisaExpiry { get; set; } public DateTime? EntryDate { get; set; }
    public MoraStatus MoraStatus { get; set; } [MaxLength(100)] public string? MoraRegistrationNumber { get; set; }
    public ComplianceLevel Compliance { get; set; } = ComplianceLevel.FullyCompliant;
    [MaxLength(20)] public string Program { get; set; } = ""; public int YearOfStudy { get; set; }
    public DateTime? ExpectedGraduation { get; set; } public Guid TenantId { get; set; } }

[Table("VisaDocuments")] public class VisaDocument { [Key] public Guid Id { get; set; }
    public Guid RegistryId { get; set; } [MaxLength(200)] public string DocumentType { get; set; } = "";
    [MaxLength(500)] public string? FileUrl { get; set; } public DateTime? ExpiryDate { get; set; }
    public bool IsVerified { get; set; } public DateTime UploadedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("VisaExpiryAlerts")] public class VisaExpiryAlert { [Key] public Guid Id { get; set; }
    public Guid RegistryId { get; set; } public Guid StudentId { get; set; }
    public ExpiryUrgency Urgency { get; set; } public int DaysRemaining { get; set; }
    [MaxLength(500)] public string? Message { get; set; } public bool IsNotified { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("VisaRenewals")] public class VisaRenewal { [Key] public Guid Id { get; set; }
    public Guid RegistryId { get; set; } public RenewalStatus Status { get; set; } = RenewalStatus.Submitted;
    [Column(TypeName = "jsonb")] public string DocumentChecklistJson { get; set; } = "[]";
    public DateTime? ApplicationDeadline { get; set; } public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; } [MaxLength(500)] public string? RejectionReason { get; set; }
    public Guid TenantId { get; set; } }



// P125 Voice Assistant (NidaBot)

// P126 Board Portal (BoardView)
[Table("ExecutiveDashboards")] public class ExecutiveDashboard { [Key] public Guid Id { get; set; }
    [MaxLength(100)] public string UserRole { get; set; } = ""; [MaxLength(20)] public string FiscalYear { get; set; } = "";
    public double AcademicExcellenceScore { get; set; } public double FinancialStabilityScore { get; set; }
    public double AccreditationScore { get; set; } public double StudentSuccessScore { get; set; }
    public double FacultyQualityScore { get; set; } public double CompositeRankingScore { get; set; }
    [Column(TypeName = "jsonb")] public string KPIsJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string StrategicGoalStatusesJson { get; set; } = "[]";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("BoardMeetingPacks")] public class BoardMeetingPack { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = ""; public DateTime MeetingDate { get; set; }
    [MaxLength(5000)] public string? ExecutiveSummary { get; set; }
    [Column(TypeName = "jsonb")] public string AgendaItemsJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string PendingDecisionsJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string PreviousResolutionsJson { get; set; } = "[]";
    [MaxLength(500)] public string? PdfUrl { get; set; } public bool IsDistributed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }


[Table("StrategicPlanGoals")] public class StrategicPlanGoal { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string Title { get; set; } = ""; [MaxLength(1000)] public string? Description { get; set; }
    public double CurrentValue { get; set; } public double TargetValue { get; set; }
    public double PercentAchieved { get; set; } public DateTime TargetDate { get; set; }
    public DateTime StartDate { get; set; } public StrategicGoalStatus Status { get; set; }
    [MaxLength(500)] public string? InterventionRecommendation { get; set; }
    public Guid TenantId { get; set; } }

[Table("BenchmarkComparisons")] public class BenchmarkComparison { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string FactorName { get; set; } = ""; public double InstitutionValue { get; set; }
    public double NationalAverage { get; set; } public double PeerAverage { get; set; }
    [MaxLength(100)] public string? PeerInstitutionName { get; set; } public bool IsAboveAverage { get; set; }
    [MaxLength(20)] public string AcademicYear { get; set; } = ""; public Guid TenantId { get; set; } }

// P127 Student Elections

[Table("CandidateNominations")] public class CandidateNomination { [Key] public Guid Id { get; set; }
    public Guid ElectionId { get; set; } public Guid StudentId { get; set; }
    [MaxLength(200)] public string PositionName { get; set; } = ""; public NominationStatus Status { get; set; } = NominationStatus.Submitted;
    public int EndorsementCount { get; set; } public int RequiredEndorsements { get; set; }
    public bool IsEligible { get; set; } [MaxLength(500)] public string? IneligibilityReason { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }


[Table("EncryptedBallots")] public class EncryptedBallot { [Key] public Guid Id { get; set; }
    public Guid ElectionId { get; set; } [MaxLength(200)] public string VoterTokenHash { get; set; } = "";
    [Column(TypeName = "text")] public string EncryptedVoteData { get; set; } = "";
    public BallotStatus Status { get; set; } = BallotStatus.Cast;
    [MaxLength(100)] public string? VerificationHash { get; set; }
    public DateTime CastAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

