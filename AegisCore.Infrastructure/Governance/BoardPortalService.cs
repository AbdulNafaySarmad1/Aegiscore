using AegisCore.Application.Governance;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Governance;

public sealed class BoardPortalService(AegisDbContext db, ILogger<BoardPortalService> logger) : IBoardPortalService
{
    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(string userRole, CancellationToken ct = default)
    {
        var health = new InstitutionHealthDto(82m, 76m, 88m, 79m, 72m, 79.4m);
        var kpis = new List<KpiEntryDto>
        {
            new("Total Enrollment", 5200m, 5500m, 4800m, "up"),
            new("Fee Collection Rate", 85m, 95m, 82m, "up"),
            new("CGPA Average", 3.12m, 3.5m, 3.05m, "up"),
            new("Faculty-to-Student Ratio", 0.04m, 0.05m, 0.038m, "up"),
            new("Graduate Employment Rate", 76m, 85m, 73m, "up"),
            new("Research Publications", 120m, 150m, 105m, "up")
        };
        var goals = await db.Set<StrategicGoal>().ToListAsync(ct);
        var goalStatuses = goals.Select(g =>
        {
            var pct = g.TargetValue > 0 ? Math.Round(g.CurrentValue / g.TargetValue * 100, 1) : 0m;
            var timePct = 50m;
            return new StrategicGoalStatusDto(g.Id, g.GoalTitle, pct >= timePct ? "on_track" : pct >= timePct * 0.8m ? "at_risk" : "off_track", pct, timePct, pct >= timePct);
        }).ToList();
        return new ExecutiveDashboardDto(health, kpis, goalStatuses);
    }

    public async Task<BoardPackDto> GenerateBoardPackAsync(Guid meetingId, CancellationToken ct = default)
    {
        var meeting = await db.Set<BoardMeeting>().FindAsync([meetingId], ct)
            ?? throw new KeyNotFoundException("Meeting not found.");
        var decisions = await db.Set<BoardDecision>().Where(d => d.MeetingId == meetingId && d.Status == "pending").ToListAsync(ct);
        var resolutions = await db.Set<BoardResolution>().Where(r => r.MeetingId == meetingId).ToListAsync(ct);
        return new BoardPackDto(meetingId, meeting.Title, meeting.ExecutiveSummary ?? "Executive Summary", decisions.Select(d => new BoardDecisionDto(d.Id, d.Title, d.Background, d.Recommendation, d.ImpactAssessment, d.Status)).ToList(), resolutions.Select(r => new BoardResolutionDto(r.Id, r.ResolutionNumber, r.Content, r.ImplementationStatus)).ToList());
    }

    public async Task<StrategicPlanStatusDto> GetStrategicPlanStatusAsync(Guid tenantId, CancellationToken ct = default)
    {
        var goals = await db.Set<StrategicGoal>().Where(g => g.TenantId == tenantId).ToListAsync(ct);
        var statuses = goals.Select(g => new StrategicGoalStatusDto(g.Id, g.GoalTitle, g.Status, g.TargetValue > 0 ? Math.Round(g.CurrentValue / g.TargetValue * 100, 1) : 0m, 50m, g.Status == "on_track")).ToList();
        return new StrategicPlanStatusDto(statuses, statuses.Count > 0 ? statuses.Average(s => s.PercentAchieved) : 0m);
    }

    public async Task<BenchmarkComparisonDto> GetBenchmarkComparisonAsync(Guid tenantId, CancellationToken ct = default)
    {
        var benchmarks = await db.Set<InstitutionBenchmark>().Where(b => b.TenantId == tenantId).ToListAsync(ct);
        return new BenchmarkComparisonDto(benchmarks.Select(b => new BenchmarkMetricDto(b.MetricName, b.InstitutionValue, b.NationalAverage, b.PeerInstitutionValue, b.PerformanceVsNational)).ToList(), "above_average", "at_par");
    }

    public async Task<IReadOnlyList<BoardAlertDto>> GetBoardAlertsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var alerts = await db.Set<BoardAlert>().Where(a => a.TenantId == tenantId && !a.IsAcknowledged).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        return alerts.Select(a => new BoardAlertDto(a.Id, a.Severity, a.Category, a.Message, a.RecommendedAction, a.ResponsibleOfficer, a.IsAcknowledged)).ToList();
    }

    public async Task<IReadOnlyList<BoardDecisionDto>> GetPendingDecisionsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var decisions = await db.Set<BoardDecision>().Where(d => d.TenantId == tenantId && d.Status == "pending").ToListAsync(ct);
        return decisions.Select(d => new BoardDecisionDto(d.Id, d.Title, d.Background, d.Recommendation, d.ImpactAssessment, d.Status)).ToList();
    }
}
