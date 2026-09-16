using System.Text.Json;
using AegisCore.Application.Governance;
using AegisCore.Application.Platform;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Governance;

/// <summary>
/// P121 accreditation readiness meter. Scores a framework from the evidence recorded against its
/// <see cref="ReadinessCriterion"/> rows (see <see cref="ReadinessScoring"/>), persists a
/// <see cref="ReadinessScore"/> with a full per-criterion breakdown, derives the open gaps and
/// alerts from the same data, and keeps <see cref="AccreditationFramework.CurrentScore"/> and the
/// score snapshot history in step so the management endpoints report the same number.
///
/// It refuses to score a framework that has no criteria: there is nothing to evidence, so no
/// number is invented.
/// </summary>
public sealed class ReadinessMeterService(AegisDbContext db) : IReadinessMeterService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ReadinessScore> ComputeScoreAsync(Guid frameworkId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var framework = await db.AccreditationFrameworks.SingleOrDefaultAsync(f => f.Id == frameworkId, ct)
            ?? throw new KeyNotFoundException("Accreditation framework not found.");
        var criteria = await db.Set<ReadinessCriterion>().Where(c => c.FrameworkId == frameworkId)
            .OrderBy(c => c.SectionName).ThenBy(c => c.CriterionCode).ToListAsync(ct);
        if (criteria.Count == 0)
            throw new InvalidOperationException($"Framework '{framework.FrameworkCode}' has no criteria recorded; a readiness score cannot be computed until its criteria are entered.");

        var criterionIds = criteria.Select(c => c.Id).ToArray();
        var evidence = await db.CriterionEvidences.Where(e => criterionIds.Contains(e.CriterionId)).ToListAsync(ct);
        var evidenceByCriterion = evidence.ToLookup(e => e.CriterionId);

        var rows = new List<CriterionRow>(criteria.Count);
        foreach (var criterion in criteria)
        {
            var result = ReadinessScoring.Evaluate(evidenceByCriterion[criterion.Id].ToList(), now);
            criterion.ComplianceStatus = result.Status;
            criterion.Score = result.Score;
            rows.Add(new CriterionRow(criterion, result));
        }

        var overall = ReadinessScoring.WeightedPercent(rows.Select(r => (r.Result.Score, r.Criterion.Weight)))
            ?? throw new InvalidOperationException($"Framework '{framework.FrameworkCode}' has no weighted criteria; a readiness score cannot be computed.");
        var categories = rows.GroupBy(r => r.Criterion.SectionName ?? "General")
            .ToDictionary(g => g.Key, g => ReadinessScoring.WeightedPercent(g.Select(r => (r.Result.Score, r.Criterion.Weight))) ?? 0m);

        var previous = await db.ReadinessScores.AsNoTracking().Where(s => s.FrameworkId == frameworkId)
            .OrderByDescending(s => s.GeneratedAt).ThenByDescending(s => s.Id).Take(24).ToListAsync(ct);
        var history = previous.OrderBy(s => s.GeneratedAt).Select(s => new HistoryPoint(s.GeneratedAt, Math.Round(s.OverallScore, 2))).ToList();
        history.Add(new HistoryPoint(now.UtcDateTime, (double)overall));

        var score = new ReadinessScore
        {
            Id = Guid.NewGuid(),
            TenantId = framework.TenantId,
            FrameworkId = framework.Id,
            FrameworkName = framework.FrameworkName.Length > 100 ? framework.FrameworkName[..100] : framework.FrameworkName,
            OverallScore = (double)overall,
            Status = ReadinessScoring.StatusFor(overall),
            CategoryScoresJson = JsonSerializer.Serialize(categories, Json),
            CriterionScoresJson = JsonSerializer.Serialize(rows.Select(r => new
            {
                criterionId = r.Criterion.Id,
                code = r.Criterion.CriterionCode,
                name = r.Criterion.Description is { Length: > 0 } d ? $"{r.Criterion.CriterionCode} — {d}" : r.Criterion.CriterionCode,
                category = r.Criterion.SectionName ?? "General",
                score = Math.Round(r.Result.Score * r.Criterion.Weight, 2),
                maxScore = r.Criterion.Weight,
                status = r.Result.Status,
                evidenceCount = r.Result.EvidenceCount,
                currentEvidenceCount = r.Result.CurrentEvidenceCount,
                evidenceSource = r.Criterion.EvidenceSource,
            }), Json),
            ScoreHistoryJson = JsonSerializer.Serialize(history, Json),
            NextAuditDate = framework.NextAuditDate?.UtcDateTime,
            DaysUntilAudit = framework.NextAuditDate is { } audit ? (int)Math.Ceiling((audit - now).TotalDays) : 0,
            ComplianceVelocity = Velocity(previous.FirstOrDefault(), overall, now),
            GeneratedAt = now.UtcDateTime,
        };
        db.ReadinessScores.Add(score);

        // Gaps: every criterion short of fully met, ranked by how many points closing it would recover.
        var totalWeight = rows.Sum(r => r.Criterion.Weight);
        var gaps = rows.Where(r => r.Result.Status != ReadinessScoring.FullyMet)
            .Select(r => new
            {
                Row = r,
                Impact = Math.Round((1m - r.Result.Score) * r.Criterion.Weight / totalWeight * 100m, 2),
            })
            .OrderByDescending(g => g.Impact).ThenBy(g => g.Row.Criterion.CriterionCode)
            .Select((g, index) => new ReadinessGap
            {
                Id = Guid.NewGuid(),
                TenantId = framework.TenantId,
                ScoreId = score.Id,
                CriterionId = g.Row.Criterion.CriterionCode,
                GapDescription = g.Row.Result.Status == ReadinessScoring.PartiallyMet
                    ? $"All evidence for {g.Row.Criterion.CriterionCode} is stale or expired."
                    : $"No evidence recorded for {g.Row.Criterion.CriterionCode}.",
                EvidenceNeeded = g.Row.Criterion.EvidenceSource ?? (g.Row.Result.Status == ReadinessScoring.PartiallyMet ? "Refreshed evidence" : "Supporting evidence"),
                Effort = g.Row.Result.Status == ReadinessScoring.PartiallyMet ? GapEffort.QuickWin : g.Row.Criterion.Weight >= 2m ? GapEffort.MediumTerm : GapEffort.ShortTerm,
                ImpactOnScore = (double)g.Impact,
                PriorityRank = index + 1,
            }).ToList();
        db.ReadinessGaps.AddRange(gaps);

        db.ReadinessAlerts.AddRange(Alerts(framework, score, rows, evidence, now));

        // Keep the management model in step: same number, same history.
        framework.CurrentScore = overall;
        db.AccreditationScoreSnapshots.Add(new AccreditationScoreSnapshot
        {
            Id = Guid.NewGuid(), TenantId = framework.TenantId, FrameworkId = framework.Id,
            Score = overall, Interpretation = ReadinessScoring.Interpretation(overall), SnapshotDate = now, CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);
        return score;
    }

    public async Task<List<ReadinessGap>> GetGapsAsync(Guid scoreId, CancellationToken ct) =>
        await db.ReadinessGaps.AsNoTracking().Where(g => g.ScoreId == scoreId).OrderBy(g => g.IsClosed).ThenBy(g => g.PriorityRank).ToListAsync(ct);

    public async Task<List<ReadinessAlert>> GetAlertsAsync(Guid scoreId, CancellationToken ct) =>
        await db.ReadinessAlerts.AsNoTracking().Where(a => a.ScoreId == scoreId).OrderByDescending(a => a.TriggeredAt).ToListAsync(ct);

    public async Task<ReadinessGap> CloseGapAsync(Guid gapId, CancellationToken ct)
    {
        var gap = await db.ReadinessGaps.SingleOrDefaultAsync(g => g.Id == gapId, ct) ?? throw new KeyNotFoundException("Gap not found.");
        if (!gap.IsClosed) { gap.IsClosed = true; gap.ClosedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
        return gap;
    }

    /// <summary>
    /// A projection, not a record: extrapolates the latest persisted score along its compliance
    /// velocity to the target date. Nothing is written, so a forecast can never surface as the
    /// institution's current score.
    /// </summary>
    public async Task<ReadinessScore> PredictReadinessAsync(Guid frameworkId, DateTime targetDate, CancellationToken ct)
    {
        var latest = await db.ReadinessScores.AsNoTracking().Where(s => s.FrameworkId == frameworkId)
            .OrderByDescending(s => s.GeneratedAt).ThenByDescending(s => s.Id).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No readiness score has been recorded for this framework; compute one before forecasting.");
        var target = targetDate.ToUniversalTime();
        var months = Math.Max(0, (target - latest.GeneratedAt).TotalDays / 30.4375);
        var projected = Math.Clamp(Math.Round(latest.OverallScore + latest.ComplianceVelocity * months, 2), 0, 100);
        return new ReadinessScore
        {
            Id = Guid.Empty, TenantId = latest.TenantId, FrameworkId = latest.FrameworkId, FrameworkName = latest.FrameworkName,
            OverallScore = projected, Status = ReadinessScoring.StatusFor((decimal)projected),
            CategoryScoresJson = "{}", CriterionScoresJson = "[]",
            ScoreHistoryJson = JsonSerializer.Serialize(new[] { new HistoryPoint(latest.GeneratedAt, latest.OverallScore), new HistoryPoint(target, projected) }, Json),
            NextAuditDate = latest.NextAuditDate,
            DaysUntilAudit = latest.NextAuditDate is { } audit ? (int)Math.Ceiling((audit - target).TotalDays) : 0,
            ComplianceVelocity = latest.ComplianceVelocity, GeneratedAt = latest.GeneratedAt,
        };
    }

    /// <summary>
    /// Re-grades every piece of criterion evidence for the tenant, flags expired items stale, and
    /// writes one freshness log entry per item.
    /// </summary>
    public async Task CheckEvidenceFreshnessAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var items = await db.CriterionEvidences.ToListAsync(ct);
        foreach (var item in items)
        {
            var status = Freshness(item, now);
            if (status == EvidenceFreshness.Expired) item.IsStale = true;
            db.EvidenceFreshnessLogs.Add(new EvidenceFreshnessLog
            {
                Id = Guid.NewGuid(), TenantId = item.TenantId, EvidenceId = item.Id, Status = status,
                LastUpdatedAt = item.EvidenceDate.UtcDateTime,
                DaysSinceUpdate = (int)(now - item.EvidenceDate).TotalDays,
                NotificationMessage = status switch
                {
                    EvidenceFreshness.Expired => $"\"{item.EvidenceTitle}\" expired on {item.ExpiryDate:yyyy-MM-dd}.",
                    EvidenceFreshness.Stale => $"\"{item.EvidenceTitle}\" is flagged stale.",
                    EvidenceFreshness.ApproachingStale => $"\"{item.EvidenceTitle}\" expires on {item.ExpiryDate:yyyy-MM-dd}.",
                    _ => null,
                },
                CheckedAt = now.UtcDateTime,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private static EvidenceFreshness Freshness(CriterionEvidence item, DateTimeOffset now)
    {
        if (item.ExpiryDate is { } expiry && expiry <= now) return EvidenceFreshness.Expired;
        if (item.IsStale) return EvidenceFreshness.Stale;
        if (item.ExpiryDate is { } soon && soon <= now.AddDays(30)) return EvidenceFreshness.ApproachingStale;
        return EvidenceFreshness.Current;
    }

    /// <summary>Points per month between the previous recorded score and this one; 0 when there is no history.</summary>
    private static double Velocity(ReadinessScore? previous, decimal current, DateTimeOffset now)
    {
        if (previous is null) return 0;
        var days = (now.UtcDateTime - previous.GeneratedAt).TotalDays;
        if (days < 1) return 0;
        return Math.Round(((double)current - previous.OverallScore) / days * 30.4375, 2);
    }

    private static IEnumerable<ReadinessAlert> Alerts(AccreditationFramework framework, ReadinessScore score, List<CriterionRow> rows, List<CriterionEvidence> evidence, DateTimeOffset now)
    {
        ReadinessAlert Make(string category, string title, string message) => new()
        {
            Id = Guid.NewGuid(), TenantId = framework.TenantId, ScoreId = score.Id, Category = category, Title = title, Message = message, TriggeredAt = now.UtcDateTime,
        };

        if (score.Status == ReadinessStatus.CriticalRisk)
            yield return Make("CRITICAL", "Readiness is at critical risk", $"{framework.FrameworkName} scored {score.OverallScore:0.#}%; below 45% the institution is unlikely to pass an audit.");
        if (framework.NextAuditDate is { } audit && audit > now && audit <= now.AddDays(90) && score.OverallScore < 75)
            yield return Make("WARNING", "Audit due with readiness below 75%", $"The {framework.FrameworkName} audit is in {score.DaysUntilAudit} days and readiness is {score.OverallScore:0.#}%.");
        if (framework.NextAuditDate is { } past && past <= now)
            yield return Make("WARNING", "Audit date has passed", $"The recorded audit date for {framework.FrameworkName} was {past:yyyy-MM-dd}; update the framework once the outcome is known.");

        var expired = evidence.Count(e => e.ExpiryDate is { } x && x <= now);
        if (expired > 0)
            yield return Make("WARNING", $"{expired} evidence item{(expired == 1 ? "" : "s")} expired", "Expired evidence no longer counts towards a criterion; upload a current version.");
        var expiring = evidence.Count(e => e.ExpiryDate is { } x && x > now && x <= now.AddDays(30));
        if (expiring > 0)
            yield return Make("INFO", $"{expiring} evidence item{(expiring == 1 ? "" : "s")} expiring within 30 days", "Renew before expiry to keep the criterion fully met.");

        var unevidenced = rows.Count(r => r.Result.Status == ReadinessScoring.NotMet);
        if (unevidenced > 0)
            yield return Make("INFO", $"{unevidenced} of {rows.Count} criteria have no evidence", "Each is listed under open gaps with the evidence it needs.");
    }

    private sealed record CriterionRow(ReadinessCriterion Criterion, ReadinessScoring.CriterionResult Result);
    private sealed record HistoryPoint(DateTime At, double Score);
}
