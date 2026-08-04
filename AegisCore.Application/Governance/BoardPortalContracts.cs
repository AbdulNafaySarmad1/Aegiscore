namespace AegisCore.Application.Governance;

public interface IBoardPortalService
{
    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(string userRole, CancellationToken ct = default);
    Task<BoardPackDto> GenerateBoardPackAsync(Guid meetingId, CancellationToken ct = default);
    Task<StrategicPlanStatusDto> GetStrategicPlanStatusAsync(Guid tenantId, CancellationToken ct = default);
    Task<BenchmarkComparisonDto> GetBenchmarkComparisonAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<BoardAlertDto>> GetBoardAlertsAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<BoardDecisionDto>> GetPendingDecisionsAsync(Guid tenantId, CancellationToken ct = default);
}

public sealed record ExecutiveDashboardDto(
    InstitutionHealthDto HealthScores,
    List<KpiEntryDto> KeyPerformanceIndicators,
    List<StrategicGoalStatusDto> StrategicGoals);

public sealed record InstitutionHealthDto(
    decimal AcademicExcellenceIndex,
    decimal FinancialStabilityScore,
    decimal AccreditationComplianceScore,
    decimal StudentSuccessScore,
    decimal FacultyQualityIndex,
    decimal CompositeRankingScore);

public sealed record KpiEntryDto(
    string KpiName,
    decimal CurrentValue,
    decimal TargetValue,
    decimal LastYearValue,
    string TrendDirection);

public sealed record StrategicGoalStatusDto(
    Guid GoalId,
    string GoalTitle,
    string Status,
    decimal PercentAchieved,
    decimal TimeElapsedPercent,
    bool IsOnTrack);

public sealed record BoardPackDto(
    Guid MeetingId,
    string MeetingTitle,
    string ExecutiveSummary,
    List<BoardDecisionDto> AgendaDecisions,
    List<BoardResolutionDto> PreviousResolutions);

public sealed record BoardDecisionDto(
    Guid Id,
    string Title,
    string? Background,
    string? Recommendation,
    string? ImpactAssessment,
    string Status);

public sealed record BoardResolutionDto(
    Guid Id,
    string ResolutionNumber,
    string? Content,
    string ImplementationStatus);

public sealed record StrategicPlanStatusDto(
    List<StrategicGoalStatusDto> Goals,
    decimal OverallProgressPercent);

public sealed record BenchmarkComparisonDto(
    List<BenchmarkMetricDto> Metrics,
    string OverallVsNational,
    string OverallVsPeer);

public sealed record BenchmarkMetricDto(
    string MetricName,
    decimal InstitutionValue,
    decimal NationalAverage,
    decimal? PeerValue,
    string PerformanceVsNational);

public sealed record BoardAlertDto(
    Guid Id,
    string Severity,
    string Category,
    string? Message,
    string? RecommendedAction,
    string? ResponsibleOfficer,
    bool IsAcknowledged);
