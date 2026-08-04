using AegisCore.Domain.Entities;

namespace AegisCore.Application.Platform;

// P121 Accreditation Readiness Score
public interface IReadinessMeterService { Task<ReadinessScore> ComputeScoreAsync(Guid frameworkId, CancellationToken ct); Task<List<ReadinessGap>> GetGapsAsync(Guid scoreId, CancellationToken ct); Task<List<ReadinessAlert>> GetAlertsAsync(Guid scoreId, CancellationToken ct); Task<ReadinessGap> CloseGapAsync(Guid gapId, CancellationToken ct); Task<ReadinessScore> PredictReadinessAsync(Guid frameworkId, DateTime targetDate, CancellationToken ct); Task CheckEvidenceFreshnessAsync(CancellationToken ct); }

// P122 Student Achievement Gamification
public interface IAchievementService { Task<List<StudentBadge>> DetectAchievementsAsync(Guid studentId, CancellationToken ct); Task<StudentBadge> IssueBadgeAsync(Guid studentId, Guid badgeId, string detail, CancellationToken ct); Task<List<DeansListEntry>> ProcessDeansListAsync(string semester, CancellationToken ct); Task<List<LeaderboardEntry>> GenerateLeaderboardAsync(LeaderboardScope scope, string scopeValue, string semester, CancellationToken ct); Task<AchievementRoadmap> GenerateRoadmapAsync(Guid studentId, CancellationToken ct); Task<List<AchievementBadge>> GetBadgeCatalogAsync(CancellationToken ct); }

// P123 Mental Health Ticketing
public interface IMentalHealthService { Task<MentalHealthTicket> SubmitTicketAsync(MentalHealthTicket ticket, CancellationToken ct); Task ClassifyAndRouteAsync(Guid ticketId, CancellationToken ct); Task AssignCounselorAsync(Guid ticketId, Guid counselorId, CancellationToken ct); Task ResolveTicketAsync(Guid ticketId, TicketResolution resolution, CancellationToken ct); Task<MandatoryReport> TriggerMandatoryReportAsync(Guid ticketId, MandatoryReportTrigger trigger, CancellationToken ct); Task<List<MentalHealthTicket>> GetCounselorQueueAsync(Guid counselorId, CancellationToken ct); Task<WellnessCheckIn> ScheduleFollowUpAsync(Guid ticketId, int daysAfter, CancellationToken ct); }

// P124 Foreign Student Visa & MORA
public interface IForeignStudentService { Task<ForeignStudentRegistry> RegisterStudentAsync(ForeignStudentRegistry registry, CancellationToken ct); Task<List<VisaExpiryAlert>> CheckExpiriesAsync(CancellationToken ct); Task<MoraSubmission> SubmitToMoraAsync(string semester, CancellationToken ct); Task<VisaRenewal> InitiateRenewalAsync(Guid registryId, CancellationToken ct); Task<ComplianceBreach> ReportBreachAsync(Guid registryId, string description, CancellationToken ct); Task<List<ForeignStudentRegistry>> GetComplianceReportAsync(CancellationToken ct); }

// P125 Voice Assistant (NidaBot)
public interface IVoiceAssistantService { Task<VoiceSession> ProcessAudioAsync(string audioInput, Guid? userId, CancellationToken ct); Task<VoiceSession> ProcessTextAsync(string textInput, Guid? userId, VoiceLanguage language, CancellationToken ct); Task<VoiceSession> ExecuteActionAsync(Guid sessionId, CancellationToken ct); Task<VoiceSession> GetSessionAsync(Guid sessionId, CancellationToken ct); }

// P126 Board Portal (BoardView)
public interface IBoardPortalService { Task<ExecutiveDashboard> GenerateDashboardAsync(string userRole, string fiscalYear, CancellationToken ct); Task<BoardMeetingPack> GenerateBoardPackAsync(string title, DateTime meetingDate, CancellationToken ct); Task<List<BoardAlert>> GetAlertsAsync(CancellationToken ct); Task<List<StrategicPlanGoal>> GetStrategicGoalsAsync(CancellationToken ct); Task<List<BenchmarkComparison>> GetBenchmarksAsync(string academicYear, CancellationToken ct); }

// P127 Student Elections
public interface IElectionService { Task<Election> CreateElectionAsync(Election election, CancellationToken ct); Task<CandidateNomination> NominateCandidateAsync(Guid electionId, Guid studentId, string position, CancellationToken ct); Task<VoterToken> IssueVoterTokenAsync(Guid electionId, Guid studentId, CancellationToken ct); Task<EncryptedBallot> CastVoteAsync(Guid electionId, string voterToken, string encryptedVote, CancellationToken ct); Task<List<ElectionResult>> TallyResultsAsync(Guid electionId, CancellationToken ct); Task<ElectionResult> CertifyResultsAsync(Guid resultId, CancellationToken ct); }

// Course Catalog Ingestion Engine
public interface ICatalogIngestionService { Task<IngestionJob> IngestDocumentAsync(string documentUrl, string documentName, CancellationToken ct); Task<IngestionJob> GetJobStatusAsync(Guid jobId, CancellationToken ct); Task<List<CatalogCourseRecord>> GetJobCoursesAsync(Guid jobId, CancellationToken ct); Task ReviewAndInsertAsync(Guid jobId, List<Guid> approvedCourseIds, CancellationToken ct); Task<CatalogVersion> PublishCatalogAsync(Guid degreeProgramId, Guid jobId, CancellationToken ct); Task<DegreeTree> ComputeDegreeTreeAsync(Guid degreeProgramId, CancellationToken ct); }

// LMS & Class Generation
public interface IClassGenerationService { Task<ClassGenerationJob> GenerateClassesAsync(string academicYear, string semester, CancellationToken ct); Task<ClassGenerationJob> GetJobStatusAsync(Guid jobId, CancellationToken ct); Task<List<Class>> GetClassesByOfferingAsync(Guid offeringId, CancellationToken ct); Task<LmsProvision> ProvisionLmsAsync(Guid classId, LmsProvider provider, CancellationToken ct); Task<LmsRosterSyncLog> SyncRosterAsync(Guid provisionId, CancellationToken ct); Task<List<LmsProvision>> GetProvisionStatusAsync(Guid classId, CancellationToken ct); }
