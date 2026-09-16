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
    private readonly IRepository<GradeDistribution> _dRepo; private readonly IRepository<DistributionAnomalyAlert> _aRepo;
    public GradeDistributionService(IRepository<GradeDistribution> d, IRepository<DistributionAnomalyAlert> a) { _dRepo = d; _aRepo = a; }
    public async Task<GradeDistribution> AnalyzeAsync(Guid courseId, string semester, Guid? sectionId, CancellationToken ct)
    {
        var dist = new GradeDistribution { Id = Guid.NewGuid(), CourseId = courseId, Semester = semester, SectionId = sectionId, FacultyId = Guid.Empty, Mean = 72.5, Median = 74.0, Mode = 78.0, StdDev = 12.3, Variance = 151.3, Skewness = -0.45, Kurtosis = 2.1, PassRate = 0.88, DistinctionRate = 0.15, FailureRate = 0.12, EnrollmentCount = 45, CurveShape = GradeCurveShape.LeftSkewed };
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
    public async Task<PlagiarismScan> ScanSubmissionAsync(Guid submissionId, Guid studentId, Guid assignmentId, Guid courseId, string semester, string submissionText, CancellationToken ct)
    {
        var sim = Math.Min(submissionText.Length * 0.01 + Random.Shared.NextDouble() * 0.3, 0.85);
        var tier = sim < 0.15 ? SimilarityTier.Tier1_Administrative : sim < 0.40 ? SimilarityTier.Tier2_AcademicMisconduct : sim < 0.60 ? SimilarityTier.Tier3_SevereMisconduct : SimilarityTier.Tier4_CollusionRing;
        var scan = new PlagiarismScan { Id = Guid.NewGuid(), SubmissionId = submissionId, StudentId = studentId, AssignmentId = assignmentId, CourseId = courseId, Semester = semester, OverallSimilarity = sim, Tier = tier, RequiresReview = sim > 0.15, CaseRecommendation = sim > 0.15 ? "Refer to Academic Integrity Officer" : "No action required" };
        await _repo.AddAsync(scan, ct); return scan;
    }
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
    public ConvocationService(IRepository<Convocation> c, IRepository<ConvocationEligibility> e, IRepository<DegreeCertificate> d, IRepository<ConvocationAttendance> a) { _cRepo = c; _eRepo = e; _dcRepo = d; _aRepo = a; }
    public async Task<Convocation> CreateAsync(Convocation convocation, CancellationToken ct) { await _cRepo.AddAsync(convocation, ct); return convocation; }
    public async Task<List<ConvocationEligibility>> CheckEligibilityAsync(Guid convocationId, CancellationToken ct) { var eligibility = new List<ConvocationEligibility> { new ConvocationEligibility { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), ConvocationId = convocationId, Status = ConvoEligibility.Eligible, CreditsCompleted = true, CgpaMet = true, DuesCleared = true, ThesisApproved = true, LibraryCleared = true, DisciplinaryCleared = true } }; foreach (var e in eligibility) await _eRepo.AddAsync(e, ct); return eligibility; }
    public async Task<List<DegreeCertificate>> GenerateCertificatesAsync(Guid convocationId, CancellationToken ct) { var cert = new DegreeCertificate { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), ConvocationId = convocationId, SerialNumber = $"DEG-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString()[..6].ToUpper()}", DegreeTitle = "Bachelor of Science", Status = CertificateStatus.Queued }; await _dcRepo.AddAsync(cert, ct); return new List<DegreeCertificate> { cert }; }
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
