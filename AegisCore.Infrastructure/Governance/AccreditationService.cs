using AegisCore.Application.Governance;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Governance;

/// <summary>
/// Management-side view of accreditation readiness. Scoring uses the same rule as the P121
/// readiness meter (<see cref="ReadinessScoring"/>); this service only reads and snapshots.
/// </summary>
public sealed class AccreditationService(AegisDbContext db, ILogger<AccreditationService> logger) : IAccreditationService
{
    public async Task<FrameworkScoreDto> ComputeFrameworkScoreAsync(Guid frameworkId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var framework = await db.Set<AccreditationFramework>().FindAsync([frameworkId], ct)
            ?? throw new InvalidOperationException("Framework not found.");
        var criteria = await db.Set<ReadinessCriterion>().Where(c => c.FrameworkId == frameworkId).ToListAsync(ct);
        var criterionIds = criteria.Select(c => c.Id).ToArray();
        var evidence = (await db.Set<CriterionEvidence>().Where(e => criterionIds.Contains(e.CriterionId)).ToListAsync(ct)).ToLookup(e => e.CriterionId);

        foreach (var criterion in criteria)
        {
            var result = ReadinessScoring.Evaluate(evidence[criterion.Id].ToList(), now);
            criterion.ComplianceStatus = result.Status;
            criterion.Score = result.Score;
        }
        var overall = ReadinessScoring.WeightedPercent(criteria.Select(c => (c.Score, c.Weight)));
        var sections = criteria.GroupBy(c => c.SectionName ?? "General").Select(g => new SectionScoreDto(
            g.Key,
            ReadinessScoring.WeightedPercent(g.Select(c => (c.Score, c.Weight))) ?? 0m,
            g.Sum(c => c.Weight),
            g.Count(c => c.ComplianceStatus == ReadinessScoring.FullyMet),
            g.Count())).ToList();

        // DateTimeOffset comparisons are filtered in memory: the SQLite provider cannot translate them and a framework has few snapshots.
        var weekAgo = now.AddDays(-7);
        var lastWeek = (await db.Set<AccreditationScoreSnapshot>().AsNoTracking().Where(s => s.FrameworkId == frameworkId).ToListAsync(ct))
            .Where(s => s.SnapshotDate <= weekAgo).OrderByDescending(s => s.SnapshotDate).Select(s => (decimal?)s.Score).FirstOrDefault();

        if (overall is { } score)
        {
            framework.CurrentScore = score;
            db.Set<AccreditationScoreSnapshot>().Add(new AccreditationScoreSnapshot
            {
                Id = Guid.NewGuid(), FrameworkId = frameworkId, Score = score,
                Interpretation = ReadinessScoring.Interpretation(score), SnapshotDate = now, CreatedAt = now, TenantId = framework.TenantId
            });
            await db.SaveChangesAsync(ct);
        }
        else
        {
            logger.LogInformation("Framework {FrameworkCode} has no criteria; no score recorded.", framework.FrameworkCode);
        }

        var daysUntil = framework.NextAuditDate.HasValue ? (int)Math.Ceiling((framework.NextAuditDate.Value - now).TotalDays) : 0;
        var current = overall ?? framework.CurrentScore;
        return new FrameworkScoreDto(frameworkId, framework.FrameworkCode, framework.FrameworkName, current,
            overall is null ? "not_assessed" : ReadinessScoring.Interpretation(current), framework.NextAuditDate, daysUntil,
            lastWeek is { } previous ? current - previous : 0m, sections);
    }

    public async Task<IReadOnlyList<FrameworkScoreDto>> GetAllFrameworkScoresAsync(Guid tenantId, CancellationToken ct = default)
    {
        var frameworks = await db.Set<AccreditationFramework>().Where(f => f.TenantId == tenantId).ToListAsync(ct);
        var results = new List<FrameworkScoreDto>();
        foreach (var fw in frameworks) results.Add(await ComputeFrameworkScoreAsync(fw.Id, ct));
        return results;
    }

    /// <summary>Read-only: derives the gap list from criterion status without writing analysis rows on every call.</summary>
    public async Task<GapAnalysisDto> GetGapAnalysisAsync(Guid frameworkId, CancellationToken ct = default)
    {
        var criteria = await db.Set<ReadinessCriterion>().AsNoTracking().Where(c => c.FrameworkId == frameworkId).ToListAsync(ct);
        var totalWeight = criteria.Sum(c => c.Weight);
        var gaps = criteria.Where(c => c.ComplianceStatus != ReadinessScoring.FullyMet)
            .Select(c => new
            {
                Criterion = c,
                Impact = totalWeight > 0 ? Math.Round((1m - c.Score) * c.Weight / totalWeight * 100m, 2) : 0m,
            })
            .OrderByDescending(g => g.Impact).ThenBy(g => g.Criterion.CriterionCode)
            .Select((g, index) => new GapItemDto(g.Criterion.Id, g.Criterion.CriterionCode,
                g.Criterion.ComplianceStatus == ReadinessScoring.PartiallyMet ? $"All evidence for {g.Criterion.CriterionCode} is stale or expired" : $"No evidence recorded for {g.Criterion.CriterionCode}",
                g.Criterion.EvidenceSource ?? "Supporting evidence", null,
                g.Criterion.ComplianceStatus == ReadinessScoring.PartiallyMet ? "quick_win" : g.Criterion.Weight >= 2m ? "medium_term" : "short_term",
                g.Impact, index + 1))
            .ToList();
        return new GapAnalysisDto(frameworkId, gaps, gaps.Sum(g => g.ImpactOnScore));
    }

    public async Task<PriorFindingsTrackerDto> GetPriorFindingsTrackerAsync(Guid frameworkId, CancellationToken ct = default)
    {
        var findings = await db.Set<AccreditationAuditFinding>().Where(f => f.FrameworkId == frameworkId).ToListAsync(ct);
        return new PriorFindingsTrackerDto(frameworkId, findings.Count, findings.Count(f => f.CorrectiveStatus == "completed"), findings.Count(f => f.CorrectiveStatus == "in_progress"), findings.Count(f => f.IsOverdue), findings.Where(f => f.IsOverdue).Select(f => new FindingDto(f.Id, f.FindingType, f.CorrectiveStatus, f.DeadlineAt, true, f.ResponsibleOwner)).ToList());
    }

    public async Task<PredictiveReadinessDto> GetPredictiveReadinessAsync(Guid frameworkId, CancellationToken ct = default)
    {
        var score = await ComputeFrameworkScoreAsync(frameworkId, ct);
        var gaps = await GetGapAnalysisAsync(frameworkId, ct);
        // Best case: every open gap closed. Worst case: every currently-met criterion whose evidence expires within 90 days lapses to partial credit.
        var now = DateTimeOffset.UtcNow;
        var horizon = now.AddDays(90);
        var criteria = await db.Set<ReadinessCriterion>().AsNoTracking().Where(c => c.FrameworkId == frameworkId).ToListAsync(ct);
        var criterionIds = criteria.Select(c => c.Id).ToArray();
        var expiringSoon = (await db.Set<CriterionEvidence>().AsNoTracking().Where(e => criterionIds.Contains(e.CriterionId) && e.ExpiryDate != null).ToListAsync(ct))
            .Where(e => e.ExpiryDate <= horizon).Select(e => e.CriterionId).Distinct().ToList();
        var totalWeight = criteria.Sum(c => c.Weight);
        var atRisk = totalWeight > 0 ? criteria.Where(c => c.ComplianceStatus == ReadinessScoring.FullyMet && expiringSoon.Contains(c.Id)).Sum(c => c.Weight * (1m - ReadinessScoring.PartialCredit)) / totalWeight * 100m : 0m;
        var best = Math.Min(100m, score.OverallScore + gaps.PotentialScoreImprovement);
        var worst = Math.Max(0m, score.OverallScore - Math.Round(atRisk, 2));
        return new PredictiveReadinessDto(frameworkId, score.OverallScore, best, worst,
            gaps.Gaps.Take(5).Select(g => g.CriterionCode).ToList(), score.OverallScore >= 75m);
    }

    public async Task<IReadOnlyList<StaleEvidenceDto>> CheckEvidenceStalenessAsync(Guid tenantId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var stale = (await db.Set<CriterionEvidence>().Where(e => e.ExpiryDate.HasValue && !e.IsStale).ToListAsync(ct)).Where(e => e.ExpiryDate!.Value < now).ToList();
        foreach (var s in stale) s.IsStale = true;
        await db.SaveChangesAsync(ct);
        var staleCriteria = stale.Select(s => s.CriterionId).Distinct().ToArray();
        var codes = await db.Set<ReadinessCriterion>().AsNoTracking().Where(c => staleCriteria.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.CriterionCode, ct);
        return stale.Select(s => new StaleEvidenceDto(s.Id, s.EvidenceTitle, s.CriterionId, codes.GetValueOrDefault(s.CriterionId, string.Empty), (int)(now - s.EvidenceDate).TotalDays, s.EvidenceDate, null)).ToList();
    }
}
