namespace AegisCore.Application.Finance;

public sealed record FinanceControlDefinition(
    string Code,
    string Title,
    string Category,
    string Severity,
    string Metric,
    string Operator,
    decimal Threshold);

public sealed record FinanceControlResult(
    string Code,
    string Title,
    string Category,
    string Severity,
    string Outcome,
    decimal Actual,
    decimal Threshold);

public sealed record CreateAuditorEngagementCommand(
    string AuditorName,
    string AuditorEmail,
    DateTime ExpiresAt,
    IReadOnlyList<string> Scopes);

public sealed record CreateAuditSampleCommand(
    string PopulationType,
    DateTime PeriodFrom,
    DateTime PeriodTo,
    int SampleSize,
    string Seed);

public sealed record GenerateSoc2EvidenceCommand(
    DateTime PeriodFrom,
    DateTime PeriodTo,
    Guid? AuditorEngagementId);

public sealed record ConfigureSodRuleCommand(
    string Code,
    string Description,
    IReadOnlyList<string> IncompatibleActions,
    bool IsActive);
