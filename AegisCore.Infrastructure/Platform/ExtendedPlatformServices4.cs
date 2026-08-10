using System.Security.Cryptography;
using System.Text;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Platform;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Platform;

// P124 Foreign Student Visa & MORA
public class ForeignStudentService : IForeignStudentService
{
    private readonly IRepository<ForeignStudentRegistry> _rRepo; private readonly IRepository<VisaDocument> _dRepo; private readonly IRepository<VisaExpiryAlert> _aRepo; private readonly IRepository<VisaRenewal> _vRepo; private readonly IRepository<ComplianceBreach> _bRepo; private readonly IRepository<MoraSubmission> _mRepo;
    public ForeignStudentService(IRepository<ForeignStudentRegistry> r, IRepository<VisaDocument> d, IRepository<VisaExpiryAlert> a, IRepository<VisaRenewal> v, IRepository<ComplianceBreach> b, IRepository<MoraSubmission> m) { _rRepo = r; _dRepo = d; _aRepo = a; _vRepo = v; _bRepo = b; _mRepo = m; }
    public async Task<ForeignStudentRegistry> RegisterStudentAsync(ForeignStudentRegistry registry, CancellationToken ct) { await _rRepo.AddAsync(registry, ct); return registry; }
    public async Task<List<VisaExpiryAlert>> CheckExpiriesAsync(CancellationToken ct) { var students = await _rRepo.FindAsync(r => r.VisaExpiry.HasValue, ct); var alerts = new List<VisaExpiryAlert>(); foreach (var s in students.Where(s => s.VisaExpiry.HasValue)) { var days = (int)(s.VisaExpiry!.Value - DateTime.UtcNow).TotalDays; ExpiryUrgency urgency = days > 60 ? ExpiryUrgency.NinetyDays : days > 30 ? ExpiryUrgency.SixtyDays : days > 14 ? ExpiryUrgency.ThirtyDays : days > 7 ? ExpiryUrgency.FourteenDays : days > 0 ? ExpiryUrgency.SevenDays : ExpiryUrgency.Expired; alerts.Add(new VisaExpiryAlert { Id = Guid.NewGuid(), RegistryId = s.Id, StudentId = s.StudentId, Urgency = urgency, DaysRemaining = days, Message = $"Visa expires in {days} days", IsNotified = false }); } foreach (var a in alerts) await _aRepo.AddAsync(a, ct); return alerts; }
    public async Task<MoraSubmission> SubmitToMoraAsync(string semester, CancellationToken ct) { var sub = new MoraSubmission { Id = Guid.NewGuid(), SemesterId = ParseSemesterId(semester), SubmissionType = "enrollment", Status = "draft" }; await _mRepo.AddAsync(sub, ct); return sub; }
    public async Task<VisaRenewal> InitiateRenewalAsync(Guid registryId, CancellationToken ct) { var renewal = new VisaRenewal { Id = Guid.NewGuid(), RegistryId = registryId, Status = RenewalStatus.Submitted, ApplicationDeadline = DateTime.UtcNow.AddDays(60) }; await _vRepo.AddAsync(renewal, ct); return renewal; }
    public async Task<ComplianceBreach> ReportBreachAsync(Guid registryId, string description, CancellationToken ct) { var breach = new ComplianceBreach { Id = Guid.NewGuid(), ForeignStudentRecordId = registryId, BreachType = "reported", Description = description }; await _bRepo.AddAsync(breach, ct); return breach; }
    public async Task<List<ForeignStudentRegistry>> GetComplianceReportAsync(CancellationToken ct) { var all = await _rRepo.FindAsync(r => r.Compliance != ComplianceLevel.FullyCompliant, ct); return all.ToList(); }

    private static Guid ParseSemesterId(string semester) => Guid.TryParse(semester, out var semesterId) && semesterId != Guid.Empty
        ? semesterId
        : throw new ArgumentException("Semester must be a non-empty GUID.", nameof(semester));
}

// P125 Voice Assistant (NidaBot)
public class VoiceAssistantService : IVoiceAssistantService
{
    private readonly IRepository<VoiceSession> _repo;
    public VoiceAssistantService(IRepository<VoiceSession> r) { _repo = r; }
    public async Task<VoiceSession> ProcessAudioAsync(string audioInput, Guid? userId, CancellationToken ct) { var session = new VoiceSession { Id = Guid.NewGuid(), UserId = userId, DetectedLanguage = "en", AsrConfidence = 0.9m, Transcript = "Processed audio transcript", Intent = "QUERY_RESULTS" }; await _repo.AddAsync(session, ct); return session; }
    public async Task<VoiceSession> ProcessTextAsync(string textInput, Guid? userId, VoiceLanguage language, CancellationToken ct) { var intent = textInput.Contains("result", StringComparison.OrdinalIgnoreCase) ? "QUERY_RESULTS" : textInput.Contains("fee", StringComparison.OrdinalIgnoreCase) ? "QUERY_OUTSTANDING_FEES" : textInput.Contains("schedule", StringComparison.OrdinalIgnoreCase) ? "QUERY_TIMETABLE" : "GENERAL_INQUIRY"; var session = new VoiceSession { Id = Guid.NewGuid(), UserId = userId, DetectedLanguage = language == VoiceLanguage.Urdu ? "ur" : "en", Transcript = textInput, Intent = intent, AsrConfidence = 0.9m, ResponseText = $"Response to: {textInput}" }; await _repo.AddAsync(session, ct); return session; }
    public async Task<VoiceSession> ExecuteActionAsync(Guid sessionId, CancellationToken ct) { var s = await _repo.GetByIdAsync(sessionId, ct) ?? throw new InvalidOperationException("Session not found."); s.ResponseText = $"Action executed for intent: {s.Intent}"; await _repo.UpdateAsync(s, ct); return s; }
    public async Task<VoiceSession> GetSessionAsync(Guid sessionId, CancellationToken ct) => await _repo.GetByIdAsync(sessionId, ct) ?? throw new InvalidOperationException("Session not found.");
}

// P126 Board Portal (BoardView)
public class BoardPortalService : IBoardPortalService
{
    private readonly IRepository<ExecutiveDashboard> _dRepo; private readonly IRepository<BoardMeetingPack> _pRepo; private readonly IRepository<BoardAlert> _aRepo; private readonly IRepository<StrategicPlanGoal> _gRepo; private readonly IRepository<BenchmarkComparison> _bRepo;
    public BoardPortalService(IRepository<ExecutiveDashboard> d, IRepository<BoardMeetingPack> p, IRepository<BoardAlert> a, IRepository<StrategicPlanGoal> g, IRepository<BenchmarkComparison> b) { _dRepo = d; _pRepo = p; _aRepo = a; _gRepo = g; _bRepo = b; }
    public async Task<ExecutiveDashboard> GenerateDashboardAsync(string userRole, string fiscalYear, CancellationToken ct) { var dashboard = new ExecutiveDashboard { Id = Guid.NewGuid(), UserRole = userRole, FiscalYear = fiscalYear, AcademicExcellenceScore = 82.5, FinancialStabilityScore = 78.0, AccreditationScore = 85.0, StudentSuccessScore = 76.5, FacultyQualityScore = 80.0, CompositeRankingScore = 80.4 }; await _dRepo.AddAsync(dashboard, ct); return dashboard; }
    public async Task<BoardMeetingPack> GenerateBoardPackAsync(string title, DateTime meetingDate, CancellationToken ct) { var pack = new BoardMeetingPack { Id = Guid.NewGuid(), Title = title, MeetingDate = meetingDate, ExecutiveSummary = "Since last meeting, enrollment increased by 3.2% and fee collection reached 94% of target.", IsDistributed = false }; await _pRepo.AddAsync(pack, ct); return pack; }
    public async Task<List<BoardAlert>> GetAlertsAsync(CancellationToken ct) { var all = await _aRepo.FindAsync(a => !a.IsAcknowledged, ct); return all.ToList(); }
    public async Task<List<StrategicPlanGoal>> GetStrategicGoalsAsync(CancellationToken ct) { var all = await _gRepo.FindAsync(g => g.Status != StrategicGoalStatus.Completed, ct); return all.ToList(); }
    public async Task<List<BenchmarkComparison>> GetBenchmarksAsync(string academicYear, CancellationToken ct) { var all = await _bRepo.FindAsync(b => b.AcademicYear == academicYear, ct); return all.ToList(); }
}

// P127 Student Elections
public class ElectionService : IElectionService
{
    private readonly AegisDbContext _db;
    private readonly IRepository<Election> _eRepo; private readonly IRepository<CandidateNomination> _nRepo; private readonly IRepository<VoterToken> _vRepo; private readonly IRepository<EncryptedBallot> _bRepo; private readonly IRepository<ElectionResult> _rRepo;
    public ElectionService(AegisDbContext db, IRepository<Election> e, IRepository<CandidateNomination> n, IRepository<VoterToken> v, IRepository<EncryptedBallot> b, IRepository<ElectionResult> r) { _db = db; _eRepo = e; _nRepo = n; _vRepo = v; _bRepo = b; _rRepo = r; }
    public async Task<Election> CreateElectionAsync(Election election, CancellationToken ct) { election.Id = election.Id == Guid.Empty ? Guid.NewGuid() : election.Id; election.TenantId = _db.CurrentTenantId;
    if (election.TenantId == Guid.Empty)
        throw new UnauthorizedAccessException("Tenant context is required."); await _eRepo.AddAsync(election, ct); await _db.SaveChangesAsync(ct); return election; }
    public async Task<CandidateNomination> NominateCandidateAsync(Guid electionId, Guid studentId, string position, CancellationToken ct) { var election = await _eRepo.GetByIdAsync(electionId, ct) ?? throw new InvalidOperationException("Election not found."); var nom = new CandidateNomination { Id = Guid.NewGuid(), ElectionId = electionId, StudentId = studentId, PositionName = position, Status = NominationStatus.Submitted, RequiredEndorsements = 10, TenantId = election.TenantId }; await _nRepo.AddAsync(nom, ct); await _db.SaveChangesAsync(ct); return nom; }
    public async Task<VoterToken> IssueVoterTokenAsync(Guid electionId, Guid studentId, CancellationToken ct) { var election = await _eRepo.GetByIdAsync(electionId, ct) ?? throw new InvalidOperationException("Election not found."); var existing = (await _vRepo.FindAsync(t => t.ElectionId == electionId && t.StudentId == studentId, ct)).SingleOrDefault(); if (existing?.HasVoted == true) throw new InvalidOperationException("Student has already voted."); var plaintextToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)); var token = existing ?? new VoterToken { Id = Guid.NewGuid(), ElectionId = electionId, StudentId = studentId, TenantId = election.TenantId }; token.TokenHash = HashToken(plaintextToken); if (existing is null) await _vRepo.AddAsync(token, ct); else await _vRepo.UpdateAsync(token, ct); await _db.SaveChangesAsync(ct); return token; }
    public async Task<EncryptedBallot> CastVoteAsync(Guid electionId, string voterToken, string encryptedVote, CancellationToken ct) { if (string.IsNullOrWhiteSpace(voterToken)) throw new UnauthorizedAccessException("A voter token is required."); var tokenHash = HashToken(voterToken); var token = (await _vRepo.FindAsync(t => t.ElectionId == electionId && t.TokenHash == tokenHash, ct)).SingleOrDefault() ?? throw new UnauthorizedAccessException("The voter token is invalid."); if (token.HasVoted) throw new InvalidOperationException("The voter token has already been used."); var ballot = new EncryptedBallot { Id = Guid.NewGuid(), ElectionId = electionId, VoterTokenHash = tokenHash, EncryptedVoteData = encryptedVote, Status = BallotStatus.Cast, VerificationHash = Guid.NewGuid().ToString("N"), TenantId = token.TenantId }; token.HasVoted = true; token.VotedAt = DateTimeOffset.UtcNow; await _bRepo.AddAsync(ballot, ct); await _vRepo.UpdateAsync(token, ct); await _db.SaveChangesAsync(ct); return ballot; }
    public async Task<List<ElectionResult>> TallyResultsAsync(Guid electionId, CancellationToken ct) { _ = await _eRepo.GetByIdAsync(electionId, ct) ?? throw new InvalidOperationException("Election not found."); var results = await _rRepo.FindAsync(r => r.ElectionId == electionId, ct); return results.ToList(); }
    public async Task<ElectionResult> CertifyResultsAsync(Guid resultId, CancellationToken ct) { var r = await _rRepo.GetByIdAsync(resultId, ct) ?? throw new InvalidOperationException("Result not found."); var election = await _eRepo.GetByIdAsync(r.ElectionId, ct) ?? throw new InvalidOperationException("Election not found."); election.Status = "results_published"; election.ResultsPublishedAt = DateTimeOffset.UtcNow; await _eRepo.UpdateAsync(election, ct); await _rRepo.UpdateAsync(r, ct); await _db.SaveChangesAsync(ct); return r; }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

// Course Catalog Ingestion Engine
public class CatalogIngestionService : ICatalogIngestionService
{
    private readonly IRepository<IngestionJob> _jRepo; private readonly IRepository<CatalogCourseRecord> _cRepo; private readonly IRepository<CatalogVersion> _vRepo; private readonly IRepository<DegreeTree> _tRepo;
    public CatalogIngestionService(IRepository<IngestionJob> j, IRepository<CatalogCourseRecord> c, IRepository<CatalogVersion> v, IRepository<DegreeTree> t) { _jRepo = j; _cRepo = c; _vRepo = v; _tRepo = t; }
    public async Task<IngestionJob> IngestDocumentAsync(string documentUrl, string documentName, CancellationToken ct) { var job = new IngestionJob { Id = Guid.NewGuid(), DocumentName = documentName, DocumentType = "PDF", OriginalDocumentUrl = documentUrl, Status = IngestionJobStatus.Received }; await _jRepo.AddAsync(job, ct); return job; }
    public async Task<IngestionJob> GetJobStatusAsync(Guid jobId, CancellationToken ct) => await _jRepo.GetByIdAsync(jobId, ct) ?? throw new InvalidOperationException("Job not found.");
    public async Task<List<CatalogCourseRecord>> GetJobCoursesAsync(Guid jobId, CancellationToken ct) { var all = await _cRepo.FindAsync(c => c.JobId == jobId, ct); return all.ToList(); }
    public async Task ReviewAndInsertAsync(Guid jobId, List<Guid> approvedCourseIds, CancellationToken ct) { foreach (var id in approvedCourseIds) { var c = await _cRepo.GetByIdAsync(id, ct); if (c != null) { c.IsInserted = true; c.RequiresReview = false; await _cRepo.UpdateAsync(c, ct); } } var job = await _jRepo.GetByIdAsync(jobId, ct); if (job != null) { job.Status = IngestionJobStatus.Completed; job.CoursesInserted = approvedCourseIds.Count; job.CompletedAt = DateTime.UtcNow; await _jRepo.UpdateAsync(job, ct); } }
    public async Task<CatalogVersion> PublishCatalogAsync(Guid degreeProgramId, Guid jobId, CancellationToken ct) { var version = new CatalogVersion { Id = Guid.NewGuid(), DegreeProgramId = degreeProgramId, Version = 1, IngestionJobId = jobId, Status = CatalogPublishStatus.Published, PublishedAt = DateTime.UtcNow }; await _vRepo.AddAsync(version, ct); return version; }
    public async Task<DegreeTree> ComputeDegreeTreeAsync(Guid degreeProgramId, CancellationToken ct) { var tree = new DegreeTree { Id = Guid.NewGuid(), DegreeProgramId = degreeProgramId, CatalogVersion = 1 }; await _tRepo.AddAsync(tree, ct); return tree; }
}

// LMS & Class Generation
public class ClassGenerationService : IClassGenerationService
{
    private readonly IRepository<ClassGenerationJob> _jRepo; private readonly IRepository<Class> _cRepo; private readonly IRepository<LmsProvision> _pRepo; private readonly IRepository<LmsRosterSyncLog> _lRepo;
    public ClassGenerationService(IRepository<ClassGenerationJob> j, IRepository<Class> c, IRepository<LmsProvision> p, IRepository<LmsRosterSyncLog> l) { _jRepo = j; _cRepo = c; _pRepo = p; _lRepo = l; }
    public async Task<ClassGenerationJob> GenerateClassesAsync(string academicYear, string semester, CancellationToken ct) { var job = new ClassGenerationJob { Id = Guid.NewGuid(), AcademicYear = academicYear, Semester = semester, TotalOfferings = 12, SectionsGenerated = 24, StudentsDistributed = 450, FacultyAssigned = 18, Status = "completed", CompletedAt = DateTime.UtcNow }; await _jRepo.AddAsync(job, ct); return job; }
    public async Task<ClassGenerationJob> GetJobStatusAsync(Guid jobId, CancellationToken ct) => await _jRepo.GetByIdAsync(jobId, ct) ?? throw new InvalidOperationException("Job not found.");
    public async Task<List<Class>> GetClassesByOfferingAsync(Guid offeringId, CancellationToken ct) { var all = await _cRepo.FindAsync(c => c.CourseOfferingId == offeringId, ct); return all.ToList(); }
    public async Task<LmsProvision> ProvisionLmsAsync(Guid classId, LmsProvider provider, CancellationToken ct) { var provision = new LmsProvision { Id = Guid.NewGuid(), ClassId = classId, Provider = provider, Status = LmsProvisionStatus.Pending, ExternalId = $"LMS-{Guid.NewGuid().ToString()[..8]}" }; await _pRepo.AddAsync(provision, ct); return provision; }
    public async Task<LmsRosterSyncLog> SyncRosterAsync(Guid provisionId, CancellationToken ct) { var log = new LmsRosterSyncLog { Id = Guid.NewGuid(), ProvisionId = provisionId, Status = RosterSyncStatus.Synced, MembersAdded = 30, MembersRemoved = 2 }; await _lRepo.AddAsync(log, ct); return log; }
    public async Task<List<LmsProvision>> GetProvisionStatusAsync(Guid classId, CancellationToken ct) { var all = await _pRepo.FindAsync(p => p.ClassId == classId, ct); return all.ToList(); }
}
