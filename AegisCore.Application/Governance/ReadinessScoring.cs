using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Governance;

/// <summary>
/// The single scoring rule for accreditation readiness. Both the P121 readiness meter and the
/// management accreditation service call this, so a framework never carries two different scores.
///
/// A criterion is scored from its evidence alone:
///   fully_met     — at least one piece of evidence that is neither flagged stale nor past expiry → 1.0
///   partially_met — evidence exists but every item is stale or expired (it was met once; needs refresh) → 0.5
///   not_met       — no evidence at all → 0.0
/// The framework score is the weight-normalised sum, expressed as a percentage.
/// </summary>
public static class ReadinessScoring
{
    public const string FullyMet = "fully_met";
    public const string PartiallyMet = "partially_met";
    public const string NotMet = "not_met";

    public const decimal PartialCredit = 0.5m;

    public sealed record CriterionResult(string Status, decimal Score, int EvidenceCount, int CurrentEvidenceCount);

    public static bool IsCurrent(CriterionEvidence evidence, DateTimeOffset now) =>
        !evidence.IsStale && (evidence.ExpiryDate is null || evidence.ExpiryDate.Value > now);

    public static CriterionResult Evaluate(IReadOnlyCollection<CriterionEvidence> evidence, DateTimeOffset now)
    {
        var current = evidence.Count(e => IsCurrent(e, now));
        if (current > 0) return new CriterionResult(FullyMet, 1.0m, evidence.Count, current);
        if (evidence.Count > 0) return new CriterionResult(PartiallyMet, PartialCredit, evidence.Count, 0);
        return new CriterionResult(NotMet, 0m, 0, 0);
    }

    /// <summary>Weight-normalised percentage; null when the total weight is zero (nothing to score).</summary>
    public static decimal? WeightedPercent(IEnumerable<(decimal Score, decimal Weight)> items)
    {
        decimal total = 0, achieved = 0;
        foreach (var (score, weight) in items) { total += weight; achieved += score * weight; }
        return total > 0 ? Math.Round(achieved / total * 100m, 2) : null;
    }

    public static ReadinessStatus StatusFor(decimal percent) => percent switch
    {
        >= 90 => ReadinessStatus.Excellent,
        >= 75 => ReadinessStatus.Good,
        >= 60 => ReadinessStatus.Satisfactory,
        >= 45 => ReadinessStatus.NeedsImprovement,
        _ => ReadinessStatus.CriticalRisk,
    };

    public static string Interpretation(decimal percent) => StatusFor(percent) switch
    {
        ReadinessStatus.Excellent => "excellent",
        ReadinessStatus.Good => "good",
        ReadinessStatus.Satisfactory => "satisfactory",
        ReadinessStatus.NeedsImprovement => "needs_improvement",
        _ => "critical_risk",
    };
}
