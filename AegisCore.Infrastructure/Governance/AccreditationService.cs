using AegisCore.Application.Governance;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Governance;

public sealed class AccreditationService(AegisDbContext db, ILogger<AccreditationService> logger) : IAccreditationService
{
    public async Task<FrameworkScoreDto> ComputeFrameworkScoreAsync(Guid frameworkId, CancellationToken ct = default)
    {
        var framework = await db.Set<AccreditationFramework>().FindAsync([frameworkId], ct)
            ?? throw new InvalidOperationException("Framework not found.");
        var criteria = await db.Set<ReadinessCriterion>().IgnoreQueryFilters()
            .Where(c => c.FrameworkId == frameworkId).ToListAsync(ct);
        foreach (var criterion in criteria)
        {
            var evidence = await db.Set<CriterionEvidence>().IgnoreQueryFilters()
                .Where(e => e.CriterionId == criterion.Id && !e.IsStale).AnyAsync(ct);
            criterion.ComplianceStatus = evidence ? "fully_met" : "not_met";
            criterion.Score = evidence ? 1.0m : 0.0m;
        }
        var totalWeight = criteria.Sum(c => c.Weight);
        var weightedScore = totalWeight > 0 ? criteria.Sum(c => c.Score * c.Weight) / totalWeight : 0m;
        framework.CurrentScore = Math.Round(weightedScore * 100, 2);
        var snapshot = new AccreditationScoreSnapshot
        {
            Id = Guid.NewGuid(), FrameworkId = frameworkId, Score = framework.CurrentScore,
            Interpretation = InterpretScore(framework.CurrentScore), TenantId = framework.TenantId
        };
        db.Set<AccreditationScoreSnapshot>().Add(snapshot);
        await db.SaveChangesAsync(ct);
        var daysUntil = framework.NextAuditDate.HasValue ? (int)(framework.NextAuditDate.Value - DateTimeOffset.UtcNow).TotalDays : 365;
        return new FrameworkScoreDto(frameworkId, framework.FrameworkCode, framework.FrameworkName, framework.CurrentScore, InterpretScore(framework.CurrentScore), framework.NextAuditDate, daysUntil, 0m, []);
    }

    public async Task<IReadOnlyList<FrameworkScoreDto>> GetAllFrameworkScoresAsync(Guid tenantId, CancellationToken ct = default)
    {
        var frameworks = await db.Set<AccreditationFramework>().IgnoreQueryFilters().Where(f => f.TenantId == tenantId).ToListAsync(ct);
        var results = new List<FrameworkScoreDto>();
        foreach (var fw in frameworks) results.Add(await ComputeFrameworkScoreAsync(fw.Id, ct));
        return results;
    }

    public async Task<GapAnalysisDto> GetGapAnalysisAsync(Guid frameworkId, CancellationToken ct = default)
    {
        var criteria = await db.Set<ReadinessCriterion>().IgnoreQueryFilters().Where(c => c.FrameworkId == frameworkId && c.ComplianceStatus != "fully_met").ToListAsync(ct);
        var gaps = criteria.Select(c =>
        {
            var gap = new GapItemDto(c.Id, c.CriterionCode, $"Evidence missing or insufficient for {c.CriterionCode}", "Upload supporting documents", null, "medium_term", c.Weight * 5m, criteria.IndexOf(c) + 1);
            var item = new GapAnalysisItem
            {
                Id = Guid.NewGuid(), CriterionId = c.Id, GapDescription = gap.GapDescription,
                EvidenceNeeded = gap.EvidenceNeeded, EstimatedEffort = gap.EstimatedEffort,
                ImpactOnScore = gap.ImpactOnScore, PriorityRank = gap.PriorityRank
            };
            db.Set<GapAnalysisItem>().Add(item);
            return gap;
        }).ToList();
        await db.SaveChangesAsync(ct);
        return new GapAnalysisDto(frameworkId, gaps, gaps.Sum(g => g.ImpactOnScore));
    }

    public async Task<PriorFindingsTrackerDto> GetPriorFindingsTrackerAsync(Guid frameworkId, CancellationToken ct = default)
    {
        var findings = await db.Set<AccreditationAuditFinding>().IgnoreQueryFilters().Where(f => f.FrameworkId == frameworkId).ToListAsync(ct);
        return new PriorFindingsTrackerDto(frameworkId, findings.Count, findings.Count(f => f.CorrectiveStatus == "completed"), findings.Count(f => f.CorrectiveStatus == "in_progress"), findings.Count(f => f.IsOverdue), findings.Where(f => f.IsOverdue).Select(f => new FindingDto(f.Id, f.FindingType, f.CorrectiveStatus, f.DeadlineAt, true, f.ResponsibleOwner)).ToList());
    }

    public async Task<PredictiveReadinessDto> GetPredictiveReadinessAsync(Guid frameworkId, CancellationToken ct = default)
    {
        var score = await ComputeFrameworkScoreAsync(frameworkId, ct);
        var b = Math.Min(100m, score.OverallScore + 5m);
        var w = Math.Max(0m, score.OverallScore - 3m);
        return new PredictiveReadinessDto(frameworkId, score.OverallScore, b, w, ["Complete missing evidence uploads"], score.OverallScore >= 75m);
    }

    public async Task<IReadOnlyList<StaleEvidenceDto>> CheckEvidenceStalenessAsync(Guid tenantId, CancellationToken ct = default)
    {
        var stale = await db.Set<CriterionEvidence>().IgnoreQueryFilters().Where(e => e.ExpiryDate.HasValue && e.ExpiryDate.Value < DateTimeOffset.UtcNow && !e.IsStale).ToListAsync(ct);
        foreach (var s in stale) s.IsStale = true;
        await db.SaveChangesAsync(ct);
        return stale.Select(s => new StaleEvidenceDto(s.Id, s.EvidenceTitle, s.CriterionId, string.Empty, (int)((DateTimeOffset.UtcNow - (s.EvidenceDate)).TotalDays), s.EvidenceDate, null)).ToList();
    }

    private static string InterpretScore(decimal score) => score switch { >= 90 => "excellent", >= 75 => "good", >= 60 => "satisfactory", >= 45 => "needs_improvement", _ => "critical_risk" };
}
