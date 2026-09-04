using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// P111 Grade Appeals
[Table("GradeAppeals")] public class GradeAppeal : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid CourseId { get; set; } [MaxLength(20)] public string Semester { get; set; } = "";
    [MaxLength(5)] public string SubmittedGrade { get; set; } = ""; [MaxLength(5000)] public string? AppealGrounds { get; set; }
    public AppealGroundType GroundType { get; set; } public EvidenceStrength GroundStrength { get; set; } = EvidenceStrength.None;
    public AppealStatus Status { get; set; } = AppealStatus.Submitted;
    public Guid FacultyRespondentId { get; set; } public Guid? CommitteeId { get; set; }
    [MaxLength(2000)] public string? CommitteeDecision { get; set; } [MaxLength(5)] public string? AdjustedGrade { get; set; }
    public DateTime SubmissionDate { get; set; } = DateTime.UtcNow; public DateTime? Deadline { get; set; }
    public DateTime? ResolvedAt { get; set; } [MaxLength(100)] public string? PolicyClauseCitation { get; set; }
    public bool IsEligible { get; set; } = true; public Guid TenantId { get; set; } }

[Table("AppealEvidence")] public class AppealEvidence : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AppealId { get; set; } [MaxLength(500)] public string? FileUrl { get; set; }
    [MaxLength(200)] public string? DocumentType { get; set; } public EvidenceRelevance Relevance { get; set; } = EvidenceRelevance.Medium;
    [MaxLength(1000)] public string? Notes { get; set; } public DateTime UploadedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("AppealCommitteeBriefs")] public class AppealCommitteeBrief : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AppealId { get; set; } [Column(TypeName = "text")] public string? BriefContent { get; set; }
    [Column(TypeName = "jsonb")] public string EvidenceMatrixJson { get; set; } = "{}";
    [MaxLength(2000)] public string? RecommendedOptions { get; set; } public DateTime GeneratedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P112 Grade Distribution Analysis
[Table("GradeDistributions")] public class GradeDistribution : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; } [MaxLength(20)] public string Semester { get; set; } = "";
    public Guid? SectionId { get; set; } public Guid FacultyId { get; set; }
    public double Mean { get; set; } public double Median { get; set; } public double? Mode { get; set; }
    public double StdDev { get; set; } public double Variance { get; set; }
    public double Skewness { get; set; } public double Kurtosis { get; set; }
    public double PassRate { get; set; } public double DistinctionRate { get; set; } public double FailureRate { get; set; }
    public int EnrollmentCount { get; set; } public GradeCurveShape CurveShape { get; set; }
    [Column(TypeName = "jsonb")] public string LetterDistributionJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string PercentileBreakdownJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string AnomaliesJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string HistoricalComparisonJson { get; set; } = "{}";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("DistributionAnomalyAlerts")] public class DistributionAnomalyAlert : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid DistributionId { get; set; } public DistributionAnomaly AnomalyType { get; set; }
    [MaxLength(500)] public string Description { get; set; } = ""; public bool IsResolved { get; set; }
    [MaxLength(300)] public string? RecommendedAction { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P113 Plagiarism Detection
[Table("PlagiarismScans")] public class PlagiarismScan : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid SubmissionId { get; set; } public Guid StudentId { get; set; } public Guid AssignmentId { get; set; }
    public Guid CourseId { get; set; } [MaxLength(20)] public string Semester { get; set; } = "";
    public double OverallSimilarity { get; set; } public SimilarityTier Tier { get; set; }
    [Column(TypeName = "jsonb")] public string VerbatimMatchesJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string ParaphraseMatchesJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string StructuralMatchesJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string StyleDeviationJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string CollusionGraphJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string AiGenerationFlagsJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string InternetMatchesJson { get; set; } = "[]";
    [MaxLength(2000)] public string? CaseRecommendation { get; set; }
    public bool RequiresReview { get; set; } public DateTime ScannedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P114 Plagiarism API Integration
[Table("PlagiarismApiSubmissions")] public class PlagiarismApiSubmission : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid SubmissionId { get; set; } public ApiProviderType Provider { get; set; }
    [MaxLength(100)] public string? ExternalSubmissionId { get; set; }
    public PlagiarismSubmissionStatus Status { get; set; } = PlagiarismSubmissionStatus.Queued;
    public double? RawSimilarity { get; set; } public double? AdjustedSimilarity { get; set; }
    public SimilarityClassification? Classification { get; set; }
    [Column(TypeName = "jsonb")] public string SourceBreakdownJson { get; set; } = "{}";
    [MaxLength(5000)] public string? FacultySummary { get; set; }
    [MaxLength(3000)] public string? StudentSummary { get; set; }
    [Column(TypeName = "jsonb")] public string ExcludedSourcesJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string ErrorLogJson { get; set; } = "{}";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow; public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; } public Guid TenantId { get; set; } }

// P115 Convocation Management
[Table("ConvocationEligibility")] public class ConvocationEligibility : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid ConvocationId { get; set; }
    public ConvoEligibility Status { get; set; } [MaxLength(1000)] public string? BlockingReason { get; set; }
    public bool CreditsCompleted { get; set; } public bool CgpaMet { get; set; } public bool DuesCleared { get; set; }
    public bool ThesisApproved { get; set; } public bool LibraryCleared { get; set; }
    public bool DisciplinaryCleared { get; set; } public Guid TenantId { get; set; } }

[Table("Convocations")] public class Convocation : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = ""; [MaxLength(20)] public string AcademicYear { get; set; } = "";
    public DateTime CeremonyDate { get; set; } [MaxLength(300)] public string? Venue { get; set; }
    public int GuestTicketsPerStudent { get; set; } = 2; public ConvocationStatus Status { get; set; } = ConvocationStatus.Planning;
    public Guid TenantId { get; set; } }

[Table("DegreeCertificates")] public class DegreeCertificate : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid ConvocationId { get; set; }
    [MaxLength(100)] public string SerialNumber { get; set; } = ""; [MaxLength(200)] public string? DegreeTitle { get; set; }
    public CertificateStatus Status { get; set; } = CertificateStatus.Queued;
    public AttestationStatus AttestationStatus { get; set; } = AttestationStatus.Pending;
    [MaxLength(100)] public string? HECReference { get; set; } public Guid TenantId { get; set; } }

[Table("ConvocationAttendance")] public class ConvocationAttendance : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid ConvocationId { get; set; }
    public CeremonyAttendance Status { get; set; } = CeremonyAttendance.Registered;
    public DateTime? CheckInTime { get; set; } public int GuestCount { get; set; }
    public bool CertificateCollected { get; set; } public Guid TenantId { get; set; } }

// P116 Semester Freeze & Deferral
[Table("SemesterFreezes")] public class SemesterFreeze : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public FreezeType Type { get; set; }
    public FreezeReasonCategory ReasonCategory { get; set; } [MaxLength(5000)] public string? ReasonDescription { get; set; }
    public FreezeStatus Status { get; set; } = FreezeStatus.Requested;
    [MaxLength(20)] public string CurrentSemester { get; set; } = ""; public int TotalSemesters { get; set; }
    public FreezeTimingRule TimingRule { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal RefundAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal OutstandingBalance { get; set; }
    public int FreezesUsed { get; set; } public int MaxFreezeSemesters { get; set; }
    [Column(TypeName = "jsonb")] public string AcademicContinuityPlanJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string EnrolledCoursesSnapshotJson { get; set; } = "[]";
    public DateTime RequestDate { get; set; } = DateTime.UtcNow; public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; } public FreezeReturnStatus? ReturnStatus { get; set; }
    public bool IsInternational { get; set; } public bool HasScholarship { get; set; }
    public Guid? ApprovedByUserId { get; set; } [MaxLength(1000)] public string? RejectionReason { get; set; } public Guid TenantId { get; set; } }

[Table("FreezeDocuments")] public class FreezeDocument : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid FreezeId { get; set; } [MaxLength(500)] public string? FileUrl { get; set; }
    [MaxLength(200)] public string? DocumentType { get; set; } public bool IsVerified { get; set; }
    [MaxLength(500)] public string? VerificationNotes { get; set; } public DateTime UploadedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P117 Online Exam Proctoring
[Table("ProctoringSessions")] public class ProctoringSession : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ExamId { get; set; } public ProctoringMode Mode { get; set; }
    public int DurationMinutes { get; set; } public bool LockdownEnabled { get; set; }
    public bool CameraRequired { get; set; } public bool MicRequired { get; set; }
    [MaxLength(20)] public string Sensitivity { get; set; } = "MEDIUM"; public DateTime ScheduledStart { get; set; }
    public DateTime? ActualStart { get; set; } public DateTime? ActualEnd { get; set; } public Guid TenantId { get; set; } }

[Table("StudentProctoringRecords")] public class StudentProctoringRecord : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid SessionId { get; set; } public Guid StudentId { get; set; }
    public StudentIntegrityStatus IntegrityStatus { get; set; } = StudentIntegrityStatus.Clean;
    public int WarningCount { get; set; }     public int CriticalCount { get; set; }
    [Column(TypeName = "jsonb")] public string EventLogJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string AnswerPatternAnalysisJson { get; set; } = "{}";
    public EnvironmentVerificationStatus VerificationStatus { get; set; } = EnvironmentVerificationStatus.NotStarted;
    [Column(TypeName = "jsonb")] public string CollusionFlagsJson { get; set; } = "[]";
    public bool IsEscalated { get; set; } public DateTime? EscalatedAt { get; set; } public Guid TenantId { get; set; } }

[Table("ProctorEvents")] public class ProctorEvent : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid RecordId { get; set; } public ProctorEventSeverity Severity { get; set; }
    [MaxLength(200)] public string EventType { get; set; } = ""; [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(500)] public string? ScreenshotUrl { get; set; } public DateTime RecordedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P118 Prerequisite Conflict Detection
[Table("PrerequisiteConflicts")] public class PrerequisiteConflict : ITenantScoped { [Key] public Guid Id { get; set; }
    public ConflictType Type { get; set; } public ConflictSeverity Severity { get; set; }
    public Guid? SourceCourseId { get; set; } public Guid? TargetCourseId { get; set; }
    public ChangeEventType? TriggerEvent { get; set; } public Guid? AffectedStudentId { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(1000)] public string? ResolutionSuggestion { get; set; }
    public bool IsResolved { get; set; } [MaxLength(500)] public string? ResolutionNotes { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow; public DateTime? ResolvedAt { get; set; } public Guid TenantId { get; set; } }

[Table("ChangeImpactSimulations")] public class ChangeImpactSimulation : ITenantScoped { [Key] public Guid Id { get; set; }
    public ChangeEventType EventType { get; set; } public Guid ChangedElementId { get; set; }
    public int DirectlyAffectedCourses { get; set; } public int IndirectlyAffectedCourses { get; set; }
    public int AffectedStudents { get; set; } public int InvalidRegistrations { get; set; }
    [Column(TypeName = "jsonb")] public string RippleEffectJson { get; set; } = "{}";
    public DateTime SimulatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P119 Fee Defaulter Prediction (extends existing FeeDefaulterRecord)
[Table("DefaulterRiskScores")] public class DefaulterRiskScore : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(20)] public string Semester { get; set; } = "";
    public double ProbabilityScore { get; set; } public DefaulterRiskTier Tier { get; set; }
    [Column(TypeName = "jsonb")] public string RiskDriversJson { get; set; } = "[]";
    public double PredictionConfidence { get; set; }
    [Column(TypeName = "jsonb")] public string PaymentBehaviorJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string AcademicCorrelationJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string FinancialProfileJson { get; set; } = "{}";
    public DateTime PredictedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("DefaulterInterventions")] public class DefaulterIntervention : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid RiskScoreId { get; set; } public Guid StudentId { get; set; }
    public DefaulterInterventionType Type { get; set; } public DefaulterRiskTier TriggeredByTier { get; set; }
    [MaxLength(2000)] public string? MessageContent { get; set; }
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow; public DateTime? CompletedAt { get; set; }
    public DefaulterInterventionOutcome? Outcome { get; set; } public bool IsSuccessful { get; set; }
    [MaxLength(500)] public string? OutcomeNotes { get; set; } public Guid TenantId { get; set; } }


// P120 Visiting Faculty Marketplace

[Table("FacultySlotMatches")] public class FacultySlotMatch : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid OpenSlotId { get; set; } public Guid FacultyProfileId { get; set; }
    public double CompositeScore { get; set; } public int Rank { get; set; }
    [Column(TypeName = "jsonb")] public string MatchRationaleJson { get; set; } = "{}";
    public bool IsSelected { get; set; } public DateTime MatchedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("VisitingFacultyContracts")] public class VisitingFacultyContract : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid FacultyProfileId { get; set; } public Guid CourseId { get; set; }
    public FacultyContractStatus Status { get; set; } = FacultyContractStatus.Draft;
    public int CreditHours { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal CompensationAmount { get; set; }
    public DateTime StartDate { get; set; } public DateTime EndDate { get; set; }
    [MaxLength(500)] public string? ContractUrl { get; set; } public DateTime? SignedAt { get; set; }
    public FacultyRehireStatus? RehireStatus { get; set; } public double? PerformanceRating { get; set; }
    [Column(TypeName = "jsonb")] public string PerformanceDetailsJson { get; set; } = "{}";
    public Guid TenantId { get; set; } }

// P121 Accreditation Readiness Score
[Table("ReadinessScores")] public class ReadinessScore : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid FrameworkId { get; set; } [MaxLength(100)] public string FrameworkName { get; set; } = "";
    public double OverallScore { get; set; } public ReadinessStatus Status { get; set; }
    [Column(TypeName = "jsonb")] public string CategoryScoresJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string CriterionScoresJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string ScoreHistoryJson { get; set; } = "[]";
    public DateTime? NextAuditDate { get; set; } public int DaysUntilAudit { get; set; }
    public double ComplianceVelocity { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("ReadinessGaps")] public class ReadinessGap : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ScoreId { get; set; } [MaxLength(100)] public string CriterionId { get; set; } = "";
    [MaxLength(1000)] public string? GapDescription { get; set; }
    [MaxLength(1000)] public string? EvidenceNeeded { get; set; }
    public Guid? ResponsibleOwnerId { get; set; } public GapEffort Effort { get; set; }
    public double ImpactOnScore { get; set; } public int PriorityRank { get; set; }
    public bool IsClosed { get; set; } public DateTime? ClosedAt { get; set; } public Guid TenantId { get; set; } }

[Table("ReadinessAlerts")] public class ReadinessAlert : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ScoreId { get; set; } [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Message { get; set; } public bool IsAcknowledged { get; set; }
    [MaxLength(20)] public string Category { get; set; } = "INFO"; public DateTime TriggeredAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("EvidenceFreshnessLogs")] public class EvidenceFreshnessLog : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid EvidenceId { get; set; } public EvidenceFreshness Status { get; set; }
    public DateTime LastUpdatedAt { get; set; } public int? DaysSinceUpdate { get; set; }
    [MaxLength(500)] public string? NotificationMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }
