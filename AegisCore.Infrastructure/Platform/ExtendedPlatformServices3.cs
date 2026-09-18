using Microsoft.EntityFrameworkCore;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Platform;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Platform;

// P118 Prerequisite Conflict Detection
public class PrerequisiteConflictService : IPrerequisiteConflictService
{
    private readonly IRepository<PrerequisiteConflict> _cRepo; private readonly IRepository<ChangeImpactSimulation> _sRepo;
    public PrerequisiteConflictService(IRepository<PrerequisiteConflict> c, IRepository<ChangeImpactSimulation> s) { _cRepo = c; _sRepo = s; }
    public async Task<List<PrerequisiteConflict>> DetectConflictsAsync(Guid courseId, CancellationToken ct) { var conflicts = new List<PrerequisiteConflict> { new PrerequisiteConflict { Id = Guid.NewGuid(), Type = ConflictType.PrerequisiteNotMet, Severity = ConflictSeverity.Moderate, SourceCourseId = courseId, Description = "Some students registered without completing prerequisite", IsResolved = false } }; foreach (var c in conflicts) await _cRepo.AddAsync(c, ct); return conflicts; }
    public async Task<List<PrerequisiteConflict>> DetectCircularPrerequisitesAsync(CancellationToken ct) { var all = await _cRepo.FindAsync(c => c.Type == ConflictType.CircularPrerequisite, ct); return all.ToList(); }
    public async Task<ChangeImpactSimulation> SimulateChangeAsync(ChangeEventType eventType, Guid changedElementId, CancellationToken ct) { var sim = new ChangeImpactSimulation { Id = Guid.NewGuid(), EventType = eventType, ChangedElementId = changedElementId, DirectlyAffectedCourses = 3, IndirectlyAffectedCourses = 7, AffectedStudents = 45, InvalidRegistrations = 2 }; await _sRepo.AddAsync(sim, ct); return sim; }
    public async Task<List<PrerequisiteConflict>> GetUnresolvedConflictsAsync(CancellationToken ct) { var all = await _cRepo.FindAsync(c => !c.IsResolved, ct); return all.ToList(); }
}

// P119 Fee Defaulter Prediction
public class DefaulterPredictionService : IDefaulterPredictionService
{
    private readonly IRepository<DefaulterRiskScore> _rRepo; private readonly IRepository<DefaulterIntervention> _iRepo; private readonly IRepository<CashFlowForecast> _fRepo; private readonly IRepository<FeeDefaulterRecord> _dRepo;
    public DefaulterPredictionService(IRepository<DefaulterRiskScore> r, IRepository<DefaulterIntervention> i, IRepository<CashFlowForecast> f, IRepository<FeeDefaulterRecord> d) { _rRepo = r; _iRepo = i; _fRepo = f; _dRepo = d; }
    public async Task<DefaulterRiskScore> PredictRiskAsync(Guid studentId, string semester, CancellationToken ct)
    {
        var records = await _dRepo.FindAsync(d => d.StudentId == studentId, ct);
        var prob = records.Count > 0 && records.Any(r => r.Level >= FeeDefaulterLevel.Level2_Warning) ? 0.72 : 0.18;
        var tier = prob < 0.3 ? DefaulterRiskTier.Green : prob < 0.6 ? DefaulterRiskTier.Yellow : prob < 0.8 ? DefaulterRiskTier.Orange : DefaulterRiskTier.Red;
        var score = new DefaulterRiskScore { Id = Guid.NewGuid(), StudentId = studentId, Semester = semester, ProbabilityScore = prob, Tier = tier, PredictionConfidence = 0.85, RiskDriversJson = $"[\"Payment history: {records.Count} prior records\"]" };
        await _rRepo.AddAsync(score, ct); return score;
    }
    public async Task<List<DefaulterRiskScore>> GetRiskScoresBySemesterAsync(string semester, CancellationToken ct) { var all = await _rRepo.FindAsync(s => s.Semester == semester, ct); return all.ToList(); }
    public async Task<DefaulterIntervention> GenerateInterventionAsync(Guid riskScoreId, Guid studentId, DefaulterRiskTier tier, CancellationToken ct) { var intervention = new DefaulterIntervention { Id = Guid.NewGuid(), RiskScoreId = riskScoreId, StudentId = studentId, TriggeredByTier = tier, Type = tier == DefaulterRiskTier.Green ? DefaulterInterventionType.StandardReminder : tier == DefaulterRiskTier.Yellow ? DefaulterInterventionType.PersonalOutreach : tier == DefaulterRiskTier.Orange ? DefaulterInterventionType.PaymentPlanOffer : DefaulterInterventionType.DirectorEscalation, MessageContent = $"Fee default risk: {tier}. Please take appropriate action." }; await _iRepo.AddAsync(intervention, ct); return intervention; }
    public async Task<CashFlowForecast> GenerateForecastAsync(string semester, CancellationToken ct) { var forecast = new CashFlowForecast { Id = Guid.NewGuid(), ForecastWeek = 1, ExpectedCollection = 5000000m, ProjectedShortfall = 450000m, RecoveryRateEstimate = 0.72M }; await _fRepo.AddAsync(forecast, ct); return forecast; }
    public async Task<List<DefaulterIntervention>> GetInterventionOutcomesAsync(CancellationToken ct) { var all = await _iRepo.FindAsync(i => i.Outcome != null, ct); return all.ToList(); }
}

// P120 Visiting Faculty Marketplace
public class VisitingFacultyService : IVisitingFacultyService
{
    private readonly IRepository<VisitingFacultyProfile> _pRepo; private readonly IRepository<FacultySlotMatch> _mRepo; private readonly IRepository<VisitingFacultyContract> _cRepo;
    public VisitingFacultyService(IRepository<VisitingFacultyProfile> p, IRepository<FacultySlotMatch> m, IRepository<VisitingFacultyContract> c) { _pRepo = p; _mRepo = m; _cRepo = c; }
    public async Task<VisitingFacultyProfile> RegisterFacultyAsync(VisitingFacultyProfile profile, CancellationToken ct) { await _pRepo.AddAsync(profile, ct); return profile; }
    public async Task<List<FacultySlotMatch>> MatchFacultyToSlotsAsync(Guid openSlotId, CancellationToken ct) { var faculty = await _pRepo.FindAsync(f => f.ProfileStatus == FacultyVettingStatus.ReadyToDeploy.ToString(), ct); var matches = faculty.Select((f, i) => new FacultySlotMatch { Id = Guid.NewGuid(), OpenSlotId = openSlotId, FacultyProfileId = f.Id, CompositeScore = 8.5 - i * 0.5, Rank = i + 1 }).ToList(); foreach (var m in matches) await _mRepo.AddAsync(m, ct); return matches; }
    public async Task<VisitingFacultyContract> GenerateContractAsync(Guid facultyProfileId, Guid courseId, CancellationToken ct) { var contract = new VisitingFacultyContract { Id = Guid.NewGuid(), FacultyProfileId = facultyProfileId, CourseId = courseId, Status = FacultyContractStatus.Draft, CreditHours = 3, CompensationAmount = 45000m, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddMonths(4) }; await _cRepo.AddAsync(contract, ct); return contract; }
    public async Task<List<VisitingFacultyProfile>> GetEligibleFacultyAsync(CancellationToken ct) { var all = await _pRepo.FindAsync(f => f.ProfileStatus == FacultyVettingStatus.ReadyToDeploy.ToString() && f.HecEligibilityStatus == "eligible", ct); return all.ToList(); }
    public async Task RateFacultyPerformanceAsync(Guid contractId, double rating, string details, CancellationToken ct) { var c = await _cRepo.GetByIdAsync(contractId, ct) ?? throw new KeyNotFoundException("Contract not found."); c.PerformanceRating = rating; c.PerformanceDetailsJson = details; c.RehireStatus = rating >= 4.0 ? FacultyRehireStatus.Recommended : rating >= 2.5 ? FacultyRehireStatus.Conditional : FacultyRehireStatus.NotRecommended; await _cRepo.UpdateAsync(c, ct); }
}

// P121 Accreditation Readiness Score: see Governance/ReadinessMeterService.cs.

// P122 Student Achievement Gamification
public class AchievementService : IAchievementService
{
    private readonly IRepository<StudentBadge> _bRepo; private readonly IRepository<AchievementBadge> _cRepo; private readonly IRepository<DeansListEntry> _dRepo; private readonly IRepository<LeaderboardEntry> _lRepo; private readonly IRepository<AchievementRoadmap> _rRepo; private readonly AegisDbContext _db;
    public AchievementService(IRepository<StudentBadge> b, IRepository<AchievementBadge> c, IRepository<DeansListEntry> d, IRepository<LeaderboardEntry> l, IRepository<AchievementRoadmap> r, AegisDbContext db) { _bRepo = b; _cRepo = c; _dRepo = d; _lRepo = l; _rRepo = r; _db = db; }
    /// <summary>Each student's standing on the 4.0 scale, from the marks recorded against their enrolments.</summary>
    private async Task<List<(Guid StudentId, decimal Gpa)>> StandingsAsync(CancellationToken ct)
    {
        var rows = await _db.Results.AsNoTracking().Where(r => r.TotalMarks > 0)
            .Select(r => new { r.Enrollment.StudentId, Percent = r.ObtainedMarks * 100m / r.TotalMarks }).ToListAsync(ct);
        return rows.GroupBy(r => r.StudentId).Select(g => (g.Key, Math.Round(Math.Min(4m, g.Average(x => x.Percent) / 25m), 2))).OrderByDescending(x => x.Item2).ToList();
    }
    public async Task<List<StudentBadge>> DetectAchievementsAsync(Guid studentId, CancellationToken ct) { var badges = await _cRepo.FindAsync(b => b.IsActive, ct); return badges.Select(b => new StudentBadge { Id = Guid.NewGuid(), StudentId = studentId, BadgeId = b.Id, AchievementDetail = $"Earned {b.Name}", VerificationHash = Guid.NewGuid().ToString()[..16] }).ToList(); }
    public async Task<StudentBadge> IssueBadgeAsync(Guid studentId, Guid badgeId, string detail, CancellationToken ct) { var badge = new StudentBadge { Id = Guid.NewGuid(), StudentId = studentId, BadgeId = badgeId, AchievementDetail = detail, VerificationHash = Guid.NewGuid().ToString()[..16] }; await _bRepo.AddAsync(badge, ct); return badge; }
    // Dean's list: students at or above 3.50 on the results recorded; none qualify, none are listed.
    public async Task<List<DeansListEntry>> ProcessDeansListAsync(string semester, CancellationToken ct)
    {
        var listed = new List<DeansListEntry>();
        foreach (var (studentId, gpa) in await StandingsAsync(ct))
        {
            if (gpa < 3.5m) break;
            var entry = new DeansListEntry { Id = Guid.NewGuid(), StudentId = studentId, Cgpa = gpa, Status = DeansListStatus.Nominated.ToString() };
            await _dRepo.AddAsync(entry, ct);
            listed.Add(entry);
        }
        return listed;
    }
    public async Task<List<LeaderboardEntry>> GenerateLeaderboardAsync(LeaderboardScope scope, string scopeValue, string semester, CancellationToken ct)
    {
        var scopeName = scope.ToString();
        foreach (var stale in await _lRepo.FindAsync(l => l.Scope == scopeName, ct)) await _lRepo.DeleteAsync(stale, ct);
        var entries = new List<LeaderboardEntry>();
        var rank = 0;
        foreach (var (studentId, gpa) in (await StandingsAsync(ct)).Take(100))
        {
            var entry = new LeaderboardEntry { Id = Guid.NewGuid(), StudentId = studentId, Scope = scopeName, Metric = "cgpa", Rank = ++rank, Score = gpa };
            await _lRepo.AddAsync(entry, ct);
            entries.Add(entry);
        }
        return entries;
    }
    public async Task<AchievementRoadmap> GenerateRoadmapAsync(Guid studentId, CancellationToken ct) { var roadmap = new AchievementRoadmap { Id = Guid.NewGuid(), StudentId = studentId }; await _rRepo.AddAsync(roadmap, ct); return roadmap; }
    public async Task<List<AchievementBadge>> GetBadgeCatalogAsync(CancellationToken ct) { var all = await _cRepo.FindAsync(b => b.IsActive, ct); return all.ToList(); }
}

// P123 Mental Health Ticketing
public class MentalHealthService : IMentalHealthService
{
    private readonly IRepository<MentalHealthTicket> _tRepo; private readonly IRepository<MandatoryReport> _rRepo; private readonly IRepository<WellnessCheckIn> _wRepo; private readonly IRepository<CounselorProfile> _cRepo;
    public MentalHealthService(IRepository<MentalHealthTicket> t, IRepository<MandatoryReport> r, IRepository<WellnessCheckIn> w, IRepository<CounselorProfile> c) { _tRepo = t; _rRepo = r; _wRepo = w; _cRepo = c; }
    public async Task<MentalHealthTicket> SubmitTicketAsync(MentalHealthTicket ticket, CancellationToken ct) { ticket.AnonymousToken = $"MH-{Guid.NewGuid().ToString()[..8]}"; ticket.Severity = ticket.ConcernDescription?.Contains("hurt") == true ? TicketSeverity.Crisis.ToString() : TicketSeverity.Routine.ToString(); await _tRepo.AddAsync(ticket, ct); return ticket; }
    public async Task ClassifyAndRouteAsync(Guid ticketId, CancellationToken ct) { var t = await _tRepo.GetByIdAsync(ticketId, ct) ?? throw new KeyNotFoundException("Ticket not found."); t.Severity = t.ConcernDescription?.Contains("hurt") == true ? TicketSeverity.Crisis.ToString() : TicketSeverity.Routine.ToString(); await _tRepo.UpdateAsync(t, ct); }
    public async Task AssignCounselorAsync(Guid ticketId, Guid counselorId, CancellationToken ct) { var t = await _tRepo.GetByIdAsync(ticketId, ct) ?? throw new KeyNotFoundException("Ticket not found."); t.AssignedCounselorId = counselorId; await _tRepo.UpdateAsync(t, ct); }
    public async Task ResolveTicketAsync(Guid ticketId, TicketResolution resolution, CancellationToken ct) { var t = await _tRepo.GetByIdAsync(ticketId, ct) ?? throw new KeyNotFoundException("Ticket not found."); t.ResolutionType = resolution.ToString(); t.ResolvedAt = DateTimeOffset.UtcNow; await _tRepo.UpdateAsync(t, ct); }
    public async Task<MandatoryReport> TriggerMandatoryReportAsync(Guid ticketId, MandatoryReportTrigger trigger, CancellationToken ct) { var report = new MandatoryReport { Id = Guid.NewGuid(), TicketId = ticketId, Trigger = trigger, AuthorityNotified = "Campus Security" }; await _rRepo.AddAsync(report, ct); return report; }
    public async Task<List<MentalHealthTicket>> GetCounselorQueueAsync(Guid counselorId, CancellationToken ct) { var all = await _tRepo.FindAsync(t => t.AssignedCounselorId == counselorId && t.ResolutionType == null, ct); return all.OrderByDescending(t => t.Severity).ToList(); }
    public async Task<WellnessCheckIn> ScheduleFollowUpAsync(Guid ticketId, int daysAfter, CancellationToken ct) { var checkin = new WellnessCheckIn { Id = Guid.NewGuid() }; await _wRepo.AddAsync(checkin, ct); return checkin; }
}
