namespace AegisCore.Application.Governance;

public interface IAccreditationService
{
    Task<FrameworkScoreDto> ComputeFrameworkScoreAsync(Guid frameworkId, CancellationToken ct = default);
    Task<IReadOnlyList<FrameworkScoreDto>> GetAllFrameworkScoresAsync(Guid tenantId, CancellationToken ct = default);
    Task<GapAnalysisDto> GetGapAnalysisAsync(Guid frameworkId, CancellationToken ct = default);
    Task<PriorFindingsTrackerDto> GetPriorFindingsTrackerAsync(Guid frameworkId, CancellationToken ct = default);
    Task<PredictiveReadinessDto> GetPredictiveReadinessAsync(Guid frameworkId, CancellationToken ct = default);
    Task<IReadOnlyList<StaleEvidenceDto>> CheckEvidenceStalenessAsync(Guid tenantId, CancellationToken ct = default);
}

public sealed record FrameworkScoreDto(
    Guid FrameworkId,
    string FrameworkCode,
    string FrameworkName,
    decimal OverallScore,
    string Interpretation,
    DateTimeOffset? NextAuditDate,
    int DaysUntilAudit,
    decimal ScoreChangeSinceLastWeek,
    List<SectionScoreDto> SectionScores);

public sealed record SectionScoreDto(
    string SectionName,
    decimal Score,
    decimal Weight,
    int CriteriaMet,
    int CriteriaTotal);

public sealed record CriterionScoreDto(
    Guid CriterionId,
    string CriterionCode,
    string ComplianceStatus,
    decimal Score,
    string? EvidenceSource);

public sealed record GapAnalysisDto(
    Guid FrameworkId,
    List<GapItemDto> Gaps,
    decimal PotentialScoreImprovement);

public sealed record GapItemDto(
    Guid CriterionId,
    string CriterionCode,
    string GapDescription,
    string? EvidenceNeeded,
    string? ResponsibleOwner,
    string EstimatedEffort,
    decimal ImpactOnScore,
    int PriorityRank);

public sealed record PriorFindingsTrackerDto(
    Guid FrameworkId,
    int TotalFindings,
    int ResolvedCount,
    int InProgressCount,
    int OverdueCount,
    List<FindingDto> OverdueFindings);

public sealed record FindingDto(
    Guid FindingId,
    string? FindingType,
    string? CorrectiveStatus,
    DateTimeOffset? Deadline,
    bool IsOverdue,
    string? ResponsibleOwner);

public sealed record PredictiveReadinessDto(
    Guid FrameworkId,
    decimal ForecastedScore,
    decimal BestCaseScore,
    decimal WorstCaseScore,
    List<string> CriticalGapsToClose,
    bool WillReachAcceptableThreshold);

public sealed record StaleEvidenceDto(
    Guid EvidenceId,
    string EvidenceTitle,
    Guid CriterionId,
    string CriterionCode,
    int DaysStale,
    DateTimeOffset LastUpdated,
    string? ResponsibleOwner);
