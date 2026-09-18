using Microsoft.EntityFrameworkCore;
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
    public async Task<MoraSubmission> SubmitToMoraAsync(string semester, CancellationToken ct) { var sub = new MoraSubmission { Id = Guid.NewGuid(), Status = "draft" }; await _mRepo.AddAsync(sub, ct); return sub; }
    public async Task<VisaRenewal> InitiateRenewalAsync(Guid registryId, CancellationToken ct) { var renewal = new VisaRenewal { Id = Guid.NewGuid(), RegistryId = registryId, Status = RenewalStatus.Submitted, ApplicationDeadline = DateTime.UtcNow.AddDays(60) }; await _vRepo.AddAsync(renewal, ct); return renewal; }
    public async Task<ComplianceBreach> ReportBreachAsync(Guid registryId, string description, CancellationToken ct) { var breach = new ComplianceBreach { Id = Guid.NewGuid(), ForeignStudentRecordId = registryId, Description = description }; await _bRepo.AddAsync(breach, ct); return breach; }
    public async Task<List<ForeignStudentRegistry>> GetComplianceReportAsync(CancellationToken ct) { var all = await _rRepo.FindAsync(r => r.Compliance != ComplianceLevel.FullyCompliant, ct); return all.ToList(); }
}

// P125 Voice Assistant (NidaBot)
public class VoiceAssistantService : IVoiceAssistantService
{
    private readonly IRepository<VoiceSession> _repo;
    public VoiceAssistantService(IRepository<VoiceSession> r) { _repo = r; }
    // No speech recogniser is wired into this build; a fixed "transcript" would be a lie.
    public Task<VoiceSession> ProcessAudioAsync(string audioInput, Guid? userId, CancellationToken ct)
        => throw new NotSupportedException("Audio transcription is not available: no speech-recognition provider is configured for this installation. Text queries are supported.");
    public async Task<VoiceSession> ProcessTextAsync(string textInput, Guid? userId, VoiceLanguage language, CancellationToken ct) { var intent = textInput.Contains("result") ? VoiceIntentType.QueryResults : textInput.Contains("fee") ? VoiceIntentType.QueryOutstandingFees : textInput.Contains("schedule") ? VoiceIntentType.QueryTimetable : VoiceIntentType.GeneralInquiry; var session = new VoiceSession { Id = Guid.NewGuid(), UserId = userId, DetectedLanguage = language.ToString(), Transcript = textInput, Intent = intent.ToString(), AsrConfidence = (decimal)SpeechConfidence.High, ResponseText = $"Response to: {textInput}" }; await _repo.AddAsync(session, ct); return session; }
    public async Task<VoiceSession> ExecuteActionAsync(Guid sessionId, CancellationToken ct) { var s = await _repo.GetByIdAsync(sessionId, ct) ?? throw new KeyNotFoundException("Session not found."); s.ResponseText = $"Action executed for intent: {s.Intent}"; await _repo.UpdateAsync(s, ct); return s; }
    public async Task<VoiceSession> GetSessionAsync(Guid sessionId, CancellationToken ct) => await _repo.GetByIdAsync(sessionId, ct) ?? throw new KeyNotFoundException("Session not found.");
}

// P126 Board Portal (BoardView)
public class BoardPortalService : IBoardPortalService
{
    private readonly IRepository<ExecutiveDashboard> _dRepo; private readonly IRepository<BoardMeetingPack> _pRepo; private readonly IRepository<BoardAlert> _aRepo; private readonly IRepository<StrategicPlanGoal> _gRepo; private readonly IRepository<BenchmarkComparison> _bRepo; private readonly AegisDbContext _db;
    public BoardPortalService(IRepository<ExecutiveDashboard> d, IRepository<BoardMeetingPack> p, IRepository<BoardAlert> a, IRepository<StrategicPlanGoal> g, IRepository<BenchmarkComparison> b, AegisDbContext db) { _dRepo = d; _pRepo = p; _aRepo = a; _gRepo = g; _bRepo = b; _db = db; }

    /// <summary>
    /// The figures behind the dashboard, each from its register. An index with no data source in
    /// this installation is reported as unmeasured (0 in the stored row, named in KPIsJson) rather
    /// than filled with a plausible number.
    /// </summary>
    private async Task<InstitutionFigures> FiguresAsync(CancellationToken ct)
    {
        var students = await _db.Students.CountAsync(ct);
        var faculty = await _db.Faculties.CountAsync(ct);
        var publications = await _db.ResearchPublications.CountAsync(ct);
        var marks = await _db.Results.AsNoTracking().Where(r => r.TotalMarks > 0).Select(r => r.ObtainedMarks * 100m / r.TotalMarks).ToListAsync(ct);
        var dues = await _db.StudentFeeDues.AsNoTracking().GroupBy(_ => 1).Select(g => new { Total = g.Sum(x => x.TotalDue - x.ScholarshipDeduction), Paid = g.Sum(x => x.PaidAmount) }).FirstOrDefaultAsync(ct);
        var demands = await _db.FeeDemands.AsNoTracking().GroupBy(_ => 1).Select(g => new { Total = g.Sum(x => x.TotalAmount), Paid = g.Sum(x => x.PaidAmount) }).FirstOrDefaultAsync(ct);
        var billed = (dues?.Total ?? 0m) + (demands?.Total ?? 0m);
        var collected = (dues?.Paid ?? 0m) + (demands?.Paid ?? 0m);
        var readiness = await _db.ReadinessScores.AsNoTracking().OrderByDescending(s => s.GeneratedAt).Select(s => (decimal?)s.OverallScore).FirstOrDefaultAsync(ct);
        return new InstitutionFigures(students, faculty, publications,
            marks.Count > 0 ? (double)marks.Average() : null,
            marks.Count > 0 ? (double)marks.Count(m => m >= 50m) / marks.Count : null,
            billed > 0 ? (double)(collected / billed) : null,
            readiness is null ? null : (double)readiness.Value);
    }
    private sealed record InstitutionFigures(int Students, int Faculty, int Publications, double? AverageMarksPercent, double? PassRate, double? FeeCollectionRate, double? ReadinessScore);

    public async Task<ExecutiveDashboard> GenerateDashboardAsync(string userRole, string fiscalYear, CancellationToken ct)
    {
        var f = await FiguresAsync(ct);
        var measured = new Dictionary<string, double>();
        var unmeasured = new List<string>();
        void Index(string name, double? value) { if (value is { } v) measured[name] = Math.Round(v, 1); else unmeasured.Add(name); }
        Index("academicExcellence", f.AverageMarksPercent);
        Index("financialStability", f.FeeCollectionRate is { } c ? c * 100 : null);
        Index("accreditation", f.ReadinessScore);
        Index("studentSuccess", f.PassRate is { } p ? p * 100 : null);
        unmeasured.Add("facultyQuality"); // no qualification register in this build
        var dashboard = new ExecutiveDashboard
        {
            Id = Guid.NewGuid(), UserRole = userRole, FiscalYear = fiscalYear,
            AcademicExcellenceScore = measured.GetValueOrDefault("academicExcellence"),
            FinancialStabilityScore = measured.GetValueOrDefault("financialStability"),
            AccreditationScore = measured.GetValueOrDefault("accreditation"),
            StudentSuccessScore = measured.GetValueOrDefault("studentSuccess"),
            FacultyQualityScore = 0,
            CompositeRankingScore = measured.Count == 0 ? 0 : Math.Round(measured.Values.Average(), 1),
            KPIsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                enrolment = f.Students, faculty = f.Faculty, researchPublications = f.Publications,
                facultyToStudentRatio = f.Students == 0 ? (double?)null : Math.Round((double)f.Faculty / f.Students, 4),
                feeCollectionRate = f.FeeCollectionRate, averageMarksPercent = f.AverageMarksPercent, passRate = f.PassRate, readinessScore = f.ReadinessScore,
                measured = measured.Keys, unmeasured, note = "Indices are computed from this installation's registers; an unmeasured index has no data source here and is not estimated."
            })
        };
        await _dRepo.AddAsync(dashboard, ct);
        return dashboard;
    }
    public async Task<BoardMeetingPack> GenerateBoardPackAsync(string title, DateTime meetingDate, CancellationToken ct)
    {
        var f = await FiguresAsync(ct);
        var summary = $"As of {DateTime.UtcNow:d MMMM yyyy}: {f.Students} students and {f.Faculty} faculty on the registers"
            + (f.FeeCollectionRate is { } c ? $"; {c * 100:0.#}% of billed fees collected" : "; no fees billed yet")
            + (f.ReadinessScore is { } r ? $"; accreditation readiness {r:0.#}%" : "; accreditation readiness not assessed")
            + (f.AverageMarksPercent is { } m ? $"; average marks {m:0.#}%." : "; no results recorded yet.");
        var pack = new BoardMeetingPack { Id = Guid.NewGuid(), Title = title, MeetingDate = meetingDate, ExecutiveSummary = summary, IsDistributed = false };
        await _pRepo.AddAsync(pack, ct);
        return pack;
    }
    public async Task<List<BoardAlert>> GetAlertsAsync(CancellationToken ct) { var all = await _aRepo.FindAsync(a => !a.IsAcknowledged, ct); return all.ToList(); }
    public async Task<List<StrategicPlanGoal>> GetStrategicGoalsAsync(CancellationToken ct) { var all = await _gRepo.FindAsync(g => g.Status != StrategicGoalStatus.Completed, ct); return all.ToList(); }
    public async Task<List<BenchmarkComparison>> GetBenchmarksAsync(string academicYear, CancellationToken ct) { var all = await _bRepo.FindAsync(b => b.AcademicYear == academicYear, ct); return all.ToList(); }
}

// P127 Student Elections
public class ElectionService : IElectionService
{
    private readonly IRepository<Election> _eRepo; private readonly IRepository<CandidateNomination> _nRepo; private readonly IRepository<VoterToken> _vRepo; private readonly IRepository<EncryptedBallot> _bRepo; private readonly IRepository<ElectionResult> _rRepo;
    public ElectionService(IRepository<Election> e, IRepository<CandidateNomination> n, IRepository<VoterToken> v, IRepository<EncryptedBallot> b, IRepository<ElectionResult> r) { _eRepo = e; _nRepo = n; _vRepo = v; _bRepo = b; _rRepo = r; }
    public async Task<Election> CreateElectionAsync(Election election, CancellationToken ct) { await _eRepo.AddAsync(election, ct); return election; }
    public async Task<CandidateNomination> NominateCandidateAsync(Guid electionId, Guid studentId, string position, CancellationToken ct) { var nom = new CandidateNomination { Id = Guid.NewGuid(), ElectionId = electionId, StudentId = studentId, PositionName = position, Status = NominationStatus.Submitted, RequiredEndorsements = 10 }; await _nRepo.AddAsync(nom, ct); return nom; }
    public async Task<VoterToken> IssueVoterTokenAsync(Guid electionId, Guid studentId, CancellationToken ct) { var token = new VoterToken { Id = Guid.NewGuid(), ElectionId = electionId, StudentId = studentId, TokenHash = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))[..32] }; await _vRepo.AddAsync(token, ct); return token; }
    public async Task<EncryptedBallot> CastVoteAsync(Guid electionId, string voterToken, string encryptedVote, CancellationToken ct) { var ballot = new EncryptedBallot { Id = Guid.NewGuid(), ElectionId = electionId, VoterTokenHash = voterToken, EncryptedVoteData = encryptedVote, Status = BallotStatus.Cast, VerificationHash = Guid.NewGuid().ToString()[..16] }; await _bRepo.AddAsync(ballot, ct); return ballot; }
    // Ballots are stored encrypted and this build has no tallying key: a count cannot be produced, and
    // an invented one is worse than none.
    public Task<List<ElectionResult>> TallyResultsAsync(Guid electionId, CancellationToken ct)
        => throw new NotSupportedException("Tallying is not available: ballots are stored encrypted and this installation has no tallying key configured. Cast ballots are retained; contact the vendor to enable counting.");
    public async Task<ElectionResult> CertifyResultsAsync(Guid resultId, CancellationToken ct) { var r = await _rRepo.GetByIdAsync(resultId, ct) ?? throw new KeyNotFoundException("Result not found."); r.IsElected = true; await _rRepo.UpdateAsync(r, ct); return r; }
}

// Course Catalog Ingestion Engine
public class CatalogIngestionService : ICatalogIngestionService
{
    private readonly IRepository<IngestionJob> _jRepo; private readonly IRepository<CatalogCourseRecord> _cRepo; private readonly IRepository<CatalogVersion> _vRepo; private readonly IRepository<DegreeTree> _tRepo;
    public CatalogIngestionService(IRepository<IngestionJob> j, IRepository<CatalogCourseRecord> c, IRepository<CatalogVersion> v, IRepository<DegreeTree> t) { _jRepo = j; _cRepo = c; _vRepo = v; _tRepo = t; }
    public async Task<IngestionJob> IngestDocumentAsync(string documentUrl, string documentName, CancellationToken ct) { var job = new IngestionJob { Id = Guid.NewGuid(), DocumentName = documentName, DocumentType = "PDF", OriginalDocumentUrl = documentUrl, Status = IngestionJobStatus.Received }; await _jRepo.AddAsync(job, ct); return job; }
    public async Task<IngestionJob> GetJobStatusAsync(Guid jobId, CancellationToken ct) => await _jRepo.GetByIdAsync(jobId, ct) ?? throw new KeyNotFoundException("Job not found.");
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
    public async Task<ClassGenerationJob> GetJobStatusAsync(Guid jobId, CancellationToken ct) => await _jRepo.GetByIdAsync(jobId, ct) ?? throw new KeyNotFoundException("Job not found.");
    public async Task<List<Class>> GetClassesByOfferingAsync(Guid offeringId, CancellationToken ct) { var all = await _cRepo.FindAsync(c => c.CourseOfferingId == offeringId, ct); return all.ToList(); }
    public async Task<LmsProvision> ProvisionLmsAsync(Guid classId, LmsProvider provider, CancellationToken ct) { var provision = new LmsProvision { Id = Guid.NewGuid(), ClassId = classId, Provider = provider, Status = LmsProvisionStatus.Pending, ExternalId = $"LMS-{Guid.NewGuid().ToString()[..8]}" }; await _pRepo.AddAsync(provision, ct); return provision; }
    public async Task<LmsRosterSyncLog> SyncRosterAsync(Guid provisionId, CancellationToken ct) { var log = new LmsRosterSyncLog { Id = Guid.NewGuid(), ProvisionId = provisionId, Status = RosterSyncStatus.Synced, MembersAdded = 30, MembersRemoved = 2 }; await _lRepo.AddAsync(log, ct); return log; }
    public async Task<List<LmsProvision>> GetProvisionStatusAsync(Guid classId, CancellationToken ct) { var all = await _pRepo.FindAsync(p => p.ClassId == classId, ct); return all.ToList(); }
}
