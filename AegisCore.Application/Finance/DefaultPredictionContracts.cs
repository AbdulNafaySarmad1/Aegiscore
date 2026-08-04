namespace AegisCore.Application.Finance;

public interface IDefaultPredictionService
{
    Task<DefaultRiskAssessmentDto> AssessStudentRiskAsync(Guid studentId, Guid semesterId, CancellationToken ct = default);
    Task<IReadOnlyList<DefaultRiskAssessmentDto>> AssessAllStudentsAsync(Guid semesterId, CancellationToken ct = default);
    Task<InterventionPlanDto> GenerateInterventionPlanAsync(Guid assessmentId, CancellationToken ct = default);
    Task<CashFlowForecastDto> GenerateCashFlowForecastAsync(Guid semesterId, CancellationToken ct = default);
    Task<TieredAlertDto> GetDefaultAlertQueueAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<InterventionOutcomeDto>> GetInterventionOutcomesAsync(Guid semesterId, CancellationToken ct = default);
}

public sealed record DefaultRiskAssessmentDto(
    Guid Id,
    Guid StudentId,
    Guid SemesterId,
    decimal DefaultProbabilityScore,
    string RiskTier,
    string? KeyRiskDrivers,
    decimal PredictionConfidence,
    decimal OutstandingAmount,
    int AverageDaysLate,
    int MaxDaysLate,
    decimal PaymentTrendScore,
    decimal ScholarshipCoveragePercent,
    bool IsHostelStudent);

public sealed record InterventionPlanDto(
    Guid AssessmentId,
    string RiskTier,
    string InterventionType,
    string? CallScript,
    bool PaymentPlanOffered,
    bool FinancialAidFlagged,
    bool AcademicAdvisorAlerted,
    bool EscalatedToDirector,
    string MessageTone);

public sealed record CashFlowForecastDto(
    Guid SemesterId,
    decimal ExpectedCollection,
    decimal ProjectedShortfall,
    decimal RecoveryRateEstimate,
    bool ShortfallAlertTriggered,
    List<WeeklyCashFlowEntry> WeeklyProjections);

public sealed record WeeklyCashFlowEntry(
    int WeekNumber,
    decimal ExpectedInflow,
    decimal ActualInflow,
    decimal Variance);

public sealed record TieredAlertDto(
    List<DefaultRiskAssessmentDto> GreenAlerts,
    List<DefaultRiskAssessmentDto> YellowAlerts,
    List<DefaultRiskAssessmentDto> OrangeAlerts,
    List<DefaultRiskAssessmentDto> RedAlerts);

public sealed record InterventionOutcomeDto(
    Guid InterventionId,
    Guid StudentId,
    string RiskTier,
    string InterventionType,
    bool WasSuccessful,
    DateTimeOffset? OutcomeAt,
    string? OutcomeNotes);
