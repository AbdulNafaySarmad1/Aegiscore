using Microsoft.EntityFrameworkCore;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Platform;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Platform;

// P111 Grade Appeals
public class GradeAppealService : IGradeAppealService
{
    private readonly IRepository<GradeAppeal> _aRepo; private readonly IRepository<AppealEvidence> _eRepo; private readonly IRepository<AppealCommitteeBrief> _bRepo;
    public GradeAppealService(IRepository<GradeAppeal> a, IRepository<AppealEvidence> e, IRepository<AppealCommitteeBrief> b) { _aRepo = a; _eRepo = e; _bRepo = b; }
    public async Task<GradeAppeal> SubmitAsync(GradeAppeal appeal, CancellationToken ct) { await _aRepo.AddAsync(appeal, ct); return appeal; }
    public async Task<List<GradeAppeal>> GetByStudentAsync(Guid studentId, CancellationToken ct) { var all = await _aRepo.FindAsync(a => a.StudentId == studentId, ct); return all.ToList(); }
    public async Task<GradeAppeal?> GetByIdAsync(Guid appealId, CancellationToken ct) => await _aRepo.GetByIdAsync(appealId, ct);
    public async Task ClassifyGroundsAsync(Guid appealId, CancellationToken ct) { var a = await _aRepo.GetByIdAsync(appealId, ct) ?? throw new KeyNotFoundException("Appeal not found."); a.GroundType = a.AppealGrounds?.Contains("error") == true ? AppealGroundType.ClericalError : a.AppealGrounds?.Contains("bias") == true ? AppealGroundType.BiasClaim : AppealGroundType.RubricMisapplication; a.GroundStrength = EvidenceStrength.Moderate; await _aRepo.UpdateAsync(a, ct); }
    public async Task<AppealCommitteeBrief> GenerateCommitteeBriefAsync(Guid appealId, CancellationToken ct) { var a = await _aRepo.GetByIdAsync(appealId, ct) ?? throw new KeyNotFoundException("Appeal not found."); var brief = new AppealCommitteeBrief { Id = Guid.NewGuid(), AppealId = appealId, BriefContent = $"Appeal for {a.CourseId} — {a.Status}", EvidenceMatrixJson = "{\"grounds\":\""+a.GroundType+"\"}", RecommendedOptions = "Review submitted evidence and grade. Consider adjustment if warranted." }; await _bRepo.AddAsync(brief, ct); return brief; }
    public async Task ProcessDecisionAsync(Guid appealId, string decision, string? adjustedGrade, CancellationToken ct) { var a = await _aRepo.GetByIdAsync(appealId, ct) ?? throw new KeyNotFoundException("Appeal not found."); a.CommitteeDecision = decision; a.Status = decision == "UPHELD" ? AppealStatus.Upheld : decision == "DISMISSED" ? AppealStatus.Dismissed : AppealStatus.PartiallyUpheld; a.AdjustedGrade = adjustedGrade; a.ResolvedAt = DateTime.UtcNow; await _aRepo.UpdateAsync(a, ct); }
    public async Task<List<GradeAppeal>> GetPendingAsync(CancellationToken ct) { var all = await _aRepo.FindAsync(a => a.Status == AppealStatus.Submitted || a.Status == AppealStatus.UnderReview, ct); return all.ToList(); }
}

// P112 Grade Distribution Analysis
public class GradeDistributionService : IGradeDistributionService
{
    private readonly IRepository<GradeDistribution> _dRepo; private readonly IRepository<DistributionAnomalyAlert> _aRepo; private readonly AegisDbContext _db;
    public GradeDistributionService(IRepository<GradeDistribution> d, IRepository<DistributionAnomalyAlert> a, AegisDbContext db) { _dRepo = d; _aRepo = a; _db = db; }
    // The statistics come from the marks recorded for the course; a course with no results has no
    // distribution, and says so rather than answering with an invented one.
    public async Task<GradeDistribution> AnalyzeAsync(Guid courseId, string semester, Guid? sectionId, CancellationToken ct)
    {
        var results = await _db.Results.AsNoTracking().Where(r => r.CourseId == courseId && r.TotalMarks > 0)
            .Select(r => new { r.ObtainedMarks, r.TotalMarks, r.Enrollment.StudentId }).ToListAsync(ct);
        if (results.Count == 0) throw new KeyNotFoundException($"No results are recorded for course {courseId}; a grade distribution cannot be computed until marks are entered.");
        var facultyId = await _db.CourseAssignments.AsNoTracking().Where(a => a.CourseId == courseId).Select(a => (Guid?)a.FacultyId).FirstOrDefaultAsync(ct) ?? Guid.Empty;
        var stats = DistributionStatistics.From(results.Select(r => (double)r.ObtainedMarks / (double)r.TotalMarks * 100).ToList());
        var dist = new GradeDistribution
        {
            Id = Guid.NewGuid(), CourseId = courseId, Semester = semester, SectionId = sectionId, FacultyId = facultyId,
            Mean = stats.Mean, Median = stats.Median, Mode = stats.Mode, StdDev = stats.StdDev, Variance = stats.Variance, Skewness = stats.Skewness, Kurtosis = stats.Kurtosis,
            PassRate = stats.Share(p => p >= 50), DistinctionRate = stats.Share(p => p >= 80), FailureRate = stats.Share(p => p < 50),
            EnrollmentCount = results.Select(r => r.StudentId).Distinct().Count(), CurveShape = stats.Shape,
            LetterDistributionJson = System.Text.Json.JsonSerializer.Serialize(stats.Letters()),
            PercentileBreakdownJson = System.Text.Json.JsonSerializer.Serialize(stats.Percentiles())
        };
        await _dRepo.AddAsync(dist, ct);
        if (dist.FailureRate > 0.35) { var alert = new DistributionAnomalyAlert { Id = Guid.NewGuid(), DistributionId = dist.Id, AnomalyType = DistributionAnomaly.HighFailureRate, Description = "Failure rate exceeds 35% threshold", RecommendedAction = "Review assessment difficulty and consider remedial support" }; await _aRepo.AddAsync(alert, ct); }
        return dist;
    }
    public async Task<List<GradeDistribution>> GetByCourseAsync(Guid courseId, CancellationToken ct) { var all = await _dRepo.FindAsync(d => d.CourseId == courseId, ct); return all.ToList(); }
    public async Task<List<DistributionAnomalyAlert>> GetAnomaliesAsync(Guid distributionId, CancellationToken ct) { var all = await _aRepo.FindAsync(a => a.DistributionId == distributionId, ct); return all.ToList(); }
}

// P113 Plagiarism Detection
public class PlagiarismDetectionService : IPlagiarismDetectionService
{
    private readonly IRepository<PlagiarismScan> _repo;
    public PlagiarismDetectionService(IRepository<PlagiarismScan> r) { _repo = r; }
    /// <summary>
    /// A similarity score is evidence in a misconduct case. This installation keeps no corpus of
    /// past submissions to compare against, so there is nothing from which to compute one: the
    /// scan is refused rather than answered with a number that means nothing. Two texts can be
    /// compared directly — see IPlagiarismEngineService.ComputeCosineSimilarityAsync.
    /// </summary>
    public Task<PlagiarismScan> ScanSubmissionAsync(Guid submissionId, Guid studentId, Guid assignmentId, Guid courseId, string semester, string submissionText, CancellationToken ct)
        => throw new NotSupportedException("Similarity scanning is not available: this installation stores no corpus of submissions to compare against, and a similarity score without one would be meaningless. Two texts can be compared directly through the text-comparison endpoint.");
    public async Task<List<PlagiarismScan>> GetByAssignmentAsync(Guid assignmentId, CancellationToken ct) { var all = await _repo.FindAsync(s => s.AssignmentId == assignmentId, ct); return all.ToList(); }
    public async Task<PlagiarismScan?> GetByIdAsync(Guid scanId, CancellationToken ct) => await _repo.GetByIdAsync(scanId, ct);
}

// P114 Plagiarism API Integration
public class PlagiarismApiService : IPlagiarismApiService
{
    private readonly IRepository<PlagiarismApiSubmission> _repo;
    public PlagiarismApiService(IRepository<PlagiarismApiSubmission> r) { _repo = r; }
    public async Task<PlagiarismApiSubmission> SubmitToProviderAsync(Guid submissionId, ApiProviderType provider, CancellationToken ct) { var sub = new PlagiarismApiSubmission { Id = Guid.NewGuid(), SubmissionId = submissionId, Provider = provider, Status = PlagiarismSubmissionStatus.Queued, ExternalSubmissionId = $"EXT-{Guid.NewGuid().ToString()[..8]}" }; await _repo.AddAsync(sub, ct); return sub; }
    public async Task<PlagiarismApiSubmission?> GetResultAsync(Guid apiSubmissionId, CancellationToken ct) => await _repo.GetByIdAsync(apiSubmissionId, ct);
    public async Task<List<PlagiarismApiSubmission>> GetBySubmissionAsync(Guid submissionId, CancellationToken ct) { var all = await _repo.FindAsync(s => s.SubmissionId == submissionId, ct); return all.ToList(); }
}

// P115 Convocation Management
public class ConvocationService : IConvocationService
{
    private readonly IRepository<Convocation> _cRepo; private readonly IRepository<ConvocationEligibility> _eRepo; private readonly IRepository<DegreeCertificate> _dcRepo; private readonly IRepository<ConvocationAttendance> _aRepo;
    private readonly AegisDbContext _db; private readonly AegisCore.Infrastructure.StudentLifecycle.GraduationService _graduation;
    public ConvocationService(IRepository<Convocation> c, IRepository<ConvocationEligibility> e, IRepository<DegreeCertificate> d, IRepository<ConvocationAttendance> a, AegisDbContext db, AegisCore.Infrastructure.StudentLifecycle.GraduationService graduation) { _cRepo = c; _eRepo = e; _dcRepo = d; _aRepo = a; _db = db; _graduation = graduation; }
    public async Task<Convocation> CreateAsync(Convocation convocation, CancellationToken ct) { await _cRepo.AddAsync(convocation, ct); return convocation; }
    /// <summary>
    /// Who may graduate at this ceremony, decided by reading each candidate's record.
    ///
    /// This used to invent a single eligibility row for a student id it had just made up, with every
    /// clearance — dues, thesis, library, discipline — ticked. A convocation office pressing "check
    /// eligibility" was shown one fictional graduand who had cleared everything.
    ///
    /// The candidates are the students whose record is complete enough to be considered: everyone
    /// not already certified for this ceremony who has finished their enrolment. Each one is put
    /// through the same evaluation the graduation screen uses — credits, CGPA, the programme's own
    /// requirements, outstanding fees, and each departmental clearance — and what blocks them is
    /// written down beside them.
    /// </summary>
    public async Task<List<ConvocationEligibility>> CheckEligibilityAsync(Guid convocationId, CancellationToken ct)
    {
        var convocation = await _cRepo.GetByIdAsync(convocationId, ct)
            ?? throw new KeyNotFoundException("That convocation is not on file.");

        var certified = await _db.DegreeCertificates.Where(c => c.ConvocationId == convocationId).Select(c => c.StudentId).ToListAsync(ct);
        var candidates = await _db.Students
            .Where(s => s.EnrollmentStatus == EnrollmentStatus.Graduated || s.EnrollmentStatus == EnrollmentStatus.Active)
            .Where(s => !certified.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (candidates.Count == 0) return [];

        var existing = await _db.ConvocationEligibilities.Where(e => e.ConvocationId == convocationId).ToListAsync(ct);
        var results = new List<ConvocationEligibility>();
        foreach (var studentId in candidates)
        {
            var progress = await _graduation.EvaluateAsync(studentId, ct);
            var blockers = progress.Blockers.ToList();
            var row = existing.FirstOrDefault(e => e.StudentId == studentId) ?? new ConvocationEligibility
            {
                Id = Guid.NewGuid(), StudentId = studentId, ConvocationId = convocationId, TenantId = convocation.TenantId
            };

            row.Status = blockers.Count == 0 ? ConvoEligibility.Eligible : ConvoEligibility.Ineligible;
            row.BlockingReason = blockers.Count == 0 ? null : Shorten(string.Join(" ", blockers), 1000);
            row.CreditsCompleted = !blockers.Any(b => b.Contains("Earned credits", StringComparison.OrdinalIgnoreCase));
            row.CgpaMet = !blockers.Any(b => b.Contains("CGPA", StringComparison.OrdinalIgnoreCase));
            row.DuesCleared = !blockers.Any(b => b.Contains("fees", StringComparison.OrdinalIgnoreCase));
            row.ThesisApproved = !blockers.Any(b => b.Contains("Requirement incomplete", StringComparison.OrdinalIgnoreCase));
            row.LibraryCleared = !blockers.Any(b => b.StartsWith("Library", StringComparison.OrdinalIgnoreCase));
            row.DisciplinaryCleared = !blockers.Any(b => b.StartsWith("Department", StringComparison.OrdinalIgnoreCase));

            if (existing.Contains(row)) await _eRepo.UpdateAsync(row, ct); else await _eRepo.AddAsync(row, ct);
            results.Add(row);
        }
        return results;
    }

    /// <summary>
    /// A certificate for each student this ceremony has found eligible — with their own degree, from
    /// their own programme. It used to make one certificate for a student id it invented, titled
    /// "Bachelor of Science" whatever the student had read.
    /// </summary>
    public async Task<List<DegreeCertificate>> GenerateCertificatesAsync(Guid convocationId, CancellationToken ct)
    {
        var convocation = await _cRepo.GetByIdAsync(convocationId, ct)
            ?? throw new KeyNotFoundException("That convocation is not on file.");

        var eligible = await _db.ConvocationEligibilities
            .Where(e => e.ConvocationId == convocationId && e.Status == ConvoEligibility.Eligible)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        if (eligible.Count == 0)
            throw new AegisCore.Infrastructure.Deployment.BusinessRuleException("No student has been found eligible for this convocation, so there is no certificate to issue. Check eligibility first; a student who is blocked is listed with the reason.");

        var already = await _db.DegreeCertificates.Where(c => c.ConvocationId == convocationId).Select(c => c.StudentId).ToListAsync(ct);
        var issued = new List<DegreeCertificate>();
        foreach (var studentId in eligible.Where(id => !already.Contains(id)))
        {
            var progress = await _graduation.EvaluateAsync(studentId, ct);
            var certificate = new DegreeCertificate
            {
                Id = Guid.NewGuid(), TenantId = convocation.TenantId, StudentId = studentId, ConvocationId = convocationId,
                SerialNumber = $"DEG-{convocation.CeremonyDate:yyyy}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                DegreeTitle = progress.Program,
                Status = CertificateStatus.Queued,
            };
            await _dcRepo.AddAsync(certificate, ct);
            issued.Add(certificate);
        }
        return issued;
    }
    /// <summary>The blocking reasons as they fit the column; the full list stays on the graduation screen.</summary>
    private static string Shorten(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";

    public async Task<ConvocationAttendance> CheckInAsync(Guid convocationId, Guid studentId, CancellationToken ct) { var att = new ConvocationAttendance { Id = Guid.NewGuid(), StudentId = studentId, ConvocationId = convocationId, Status = CeremonyAttendance.CheckedIn, CheckInTime = DateTime.UtcNow }; await _aRepo.AddAsync(att, ct); return att; }
    public async Task<List<Convocation>> GetUpcomingAsync(CancellationToken ct) { var all = await _cRepo.FindAsync(c => c.CeremonyDate > DateTime.UtcNow, ct); return all.ToList(); }
}

// P116 Semester Freeze & Deferral
public class SemesterFreezeService : ISemesterFreezeService
{
    private readonly IRepository<SemesterFreeze> _repo; private readonly IRepository<FreezeDocument> _dRepo;
    public SemesterFreezeService(IRepository<SemesterFreeze> r, IRepository<FreezeDocument> d) { _repo = r; _dRepo = d; }
    public async Task<SemesterFreeze> ApplyAsync(SemesterFreeze freeze, CancellationToken ct) { freeze.TimingRule = DateTime.UtcNow.Day <= 14 ? FreezeTimingRule.Week1to2_FullRefund : FreezeTimingRule.Week3to4_PartialRefund; await _repo.AddAsync(freeze, ct); return freeze; }
    public async Task<SemesterFreeze?> GetByIdAsync(Guid freezeId, CancellationToken ct) => await _repo.GetByIdAsync(freezeId, ct);
    public async Task<List<SemesterFreeze>> GetByStudentAsync(Guid studentId, CancellationToken ct) { var all = await _repo.FindAsync(f => f.StudentId == studentId, ct); return all.ToList(); }
    public async Task ProcessApprovalAsync(Guid freezeId, bool approved, string? rejectionReason, Guid? approvedByUserId, CancellationToken ct) { var f = await _repo.GetByIdAsync(freezeId, ct) ?? throw new KeyNotFoundException("Freeze request not found."); f.Status = approved ? FreezeStatus.Approved : FreezeStatus.Rejected; f.ApprovedByUserId = approvedByUserId; f.RejectionReason = rejectionReason; f.EffectiveDate = approved ? DateTime.UtcNow : null; await _repo.UpdateAsync(f, ct); }
    public async Task InitiateReturnAsync(Guid freezeId, CancellationToken ct) { var f = await _repo.GetByIdAsync(freezeId, ct) ?? throw new KeyNotFoundException("Freeze not found."); f.ReturnStatus = FreezeReturnStatus.Notified; await _repo.UpdateAsync(f, ct); }
}

// P117 Online Exam Proctoring
public class ProctoringService : IProctoringService
{
    private readonly IRepository<ProctoringSession> _sRepo; private readonly IRepository<StudentProctoringRecord> _rRepo; private readonly IRepository<ProctorEvent> _eRepo;
    public ProctoringService(IRepository<ProctoringSession> s, IRepository<StudentProctoringRecord> r, IRepository<ProctorEvent> e) { _sRepo = s; _rRepo = r; _eRepo = e; }
    public async Task<ProctoringSession> CreateSessionAsync(ProctoringSession session, CancellationToken ct) { await _sRepo.AddAsync(session, ct); return session; }
    public async Task<StudentProctoringRecord> VerifyStudentEnvironmentAsync(Guid sessionId, Guid studentId, CancellationToken ct) { var rec = new StudentProctoringRecord { Id = Guid.NewGuid(), SessionId = sessionId, StudentId = studentId, VerificationStatus = EnvironmentVerificationStatus.Passed, IntegrityStatus = StudentIntegrityStatus.Clean }; await _rRepo.AddAsync(rec, ct); return rec; }
    public async Task LogProctorEventAsync(Guid recordId, ProctorEventSeverity severity, string eventType, string? description, CancellationToken ct) { var ev = new ProctorEvent { Id = Guid.NewGuid(), RecordId = recordId, Severity = severity, EventType = eventType, Description = description }; await _eRepo.AddAsync(ev, ct); }
    public async Task<StudentProctoringRecord> CompleteProctoringAsync(Guid recordId, CancellationToken ct) { var r = await _rRepo.GetByIdAsync(recordId, ct) ?? throw new KeyNotFoundException("Record not found."); r.IntegrityStatus = r.CriticalCount > 0 ? StudentIntegrityStatus.ReferredToOfficer : StudentIntegrityStatus.Clean; await _rRepo.UpdateAsync(r, ct); return r; }
    public async Task<List<StudentProctoringRecord>> GetSessionRecordsAsync(Guid sessionId, CancellationToken ct) { var all = await _rRepo.FindAsync(r => r.SessionId == sessionId, ct); return all.ToList(); }
}
