using AegisCore.Domain.Entities;

namespace AegisCore.Application.Platform;

// P111 Grade Appeals
public interface IGradeAppealService { Task<GradeAppeal> SubmitAsync(GradeAppeal appeal, CancellationToken ct); Task<List<GradeAppeal>> GetByStudentAsync(Guid studentId, CancellationToken ct); Task<GradeAppeal?> GetByIdAsync(Guid appealId, CancellationToken ct); Task ClassifyGroundsAsync(Guid appealId, CancellationToken ct); Task<AppealCommitteeBrief> GenerateCommitteeBriefAsync(Guid appealId, CancellationToken ct); Task ProcessDecisionAsync(Guid appealId, string decision, string? adjustedGrade, CancellationToken ct); Task<List<GradeAppeal>> GetPendingAsync(CancellationToken ct); }

// P112 Grade Distribution Analysis
public interface IGradeDistributionService { Task<GradeDistribution> AnalyzeAsync(Guid courseId, string semester, Guid? sectionId, CancellationToken ct); Task<List<GradeDistribution>> GetByCourseAsync(Guid courseId, CancellationToken ct); Task<List<DistributionAnomalyAlert>> GetAnomaliesAsync(Guid distributionId, CancellationToken ct); }

// P113 Plagiarism Detection
public interface IPlagiarismDetectionService { Task<PlagiarismScan> ScanSubmissionAsync(Guid submissionId, Guid studentId, Guid assignmentId, Guid courseId, string semester, string submissionText, CancellationToken ct); Task<List<PlagiarismScan>> GetByAssignmentAsync(Guid assignmentId, CancellationToken ct); Task<PlagiarismScan?> GetByIdAsync(Guid scanId, CancellationToken ct); }

// P114 Plagiarism API Integration
public interface IPlagiarismApiService { Task<PlagiarismApiSubmission> SubmitToProviderAsync(Guid submissionId, ApiProviderType provider, CancellationToken ct); Task<PlagiarismApiSubmission?> GetResultAsync(Guid apiSubmissionId, CancellationToken ct); Task<List<PlagiarismApiSubmission>> GetBySubmissionAsync(Guid submissionId, CancellationToken ct); }

// P115 Convocation Management
public interface IConvocationService { Task<Convocation> CreateAsync(Convocation convocation, CancellationToken ct); Task<List<ConvocationEligibility>> CheckEligibilityAsync(Guid convocationId, CancellationToken ct); Task<List<DegreeCertificate>> GenerateCertificatesAsync(Guid convocationId, CancellationToken ct); Task<ConvocationAttendance> CheckInAsync(Guid convocationId, Guid studentId, CancellationToken ct); Task<List<Convocation>> GetUpcomingAsync(CancellationToken ct); }

// P116 Semester Freeze & Deferral
public interface ISemesterFreezeService { Task<SemesterFreeze> ApplyAsync(SemesterFreeze freeze, CancellationToken ct); Task<SemesterFreeze?> GetByIdAsync(Guid freezeId, CancellationToken ct); Task<List<SemesterFreeze>> GetByStudentAsync(Guid studentId, CancellationToken ct); Task ProcessApprovalAsync(Guid freezeId, bool approved, string? rejectionReason, Guid? approvedByUserId, CancellationToken ct); Task InitiateReturnAsync(Guid freezeId, CancellationToken ct); }

// P117 Online Exam Proctoring
public interface IProctoringService { Task<ProctoringSession> CreateSessionAsync(ProctoringSession session, CancellationToken ct); Task<StudentProctoringRecord> VerifyStudentEnvironmentAsync(Guid sessionId, Guid studentId, CancellationToken ct); Task LogProctorEventAsync(Guid recordId, ProctorEventSeverity severity, string eventType, string? description, CancellationToken ct); Task<StudentProctoringRecord> CompleteProctoringAsync(Guid recordId, CancellationToken ct); Task<List<StudentProctoringRecord>> GetSessionRecordsAsync(Guid sessionId, CancellationToken ct); }

// P118 Prerequisite Conflict Detection
public interface IPrerequisiteConflictService { Task<List<PrerequisiteConflict>> DetectConflictsAsync(Guid courseId, CancellationToken ct); Task<List<PrerequisiteConflict>> DetectCircularPrerequisitesAsync(CancellationToken ct); Task<ChangeImpactSimulation> SimulateChangeAsync(ChangeEventType eventType, Guid changedElementId, CancellationToken ct); Task<List<PrerequisiteConflict>> GetUnresolvedConflictsAsync(CancellationToken ct); }

// P119 Fee Defaulter Prediction (extends P103)
public interface IDefaulterPredictionService { Task<DefaulterRiskScore> PredictRiskAsync(Guid studentId, string semester, CancellationToken ct); Task<List<DefaulterRiskScore>> GetRiskScoresBySemesterAsync(string semester, CancellationToken ct); Task<DefaulterIntervention> GenerateInterventionAsync(Guid riskScoreId, Guid studentId, DefaulterRiskTier tier, CancellationToken ct); Task<CashFlowForecast> GenerateForecastAsync(string semester, CancellationToken ct); Task<List<DefaulterIntervention>> GetInterventionOutcomesAsync(CancellationToken ct); }

// P120 Visiting Faculty Marketplace
public interface IVisitingFacultyService { Task<VisitingFacultyProfile> RegisterFacultyAsync(VisitingFacultyProfile profile, CancellationToken ct); Task<List<FacultySlotMatch>> MatchFacultyToSlotsAsync(Guid openSlotId, CancellationToken ct); Task<VisitingFacultyContract> GenerateContractAsync(Guid facultyProfileId, Guid courseId, CancellationToken ct); Task<List<VisitingFacultyProfile>> GetEligibleFacultyAsync(CancellationToken ct); Task RateFacultyPerformanceAsync(Guid contractId, double rating, string details, CancellationToken ct); }
