using System.Text.Json.Serialization;

namespace AegisCore.Application.Analytics;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExplanationSource
{
    Rule,
    Statistic,
    Model,
    Human,
    Mixed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExplanationKind
{
    DeterministicDecision,
    StatisticalObservation,
    MlPrediction,
    HumanDecision
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AvailabilityStatus
{
    Available,
    Partial,
    Unavailable,
    Suppressed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PerformanceStatus
{
    Exceptional,
    Strong,
    Developing,
    AtRisk,
    Unavailable
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImprovementStatus
{
    PersonalBest,
    Improving,
    Stable,
    Declining,
    Unavailable
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TrendDirection
{
    Up,
    Flat,
    Down,
    Unavailable
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModelKind
{
    GenuinePredictiveModel,
    DeterministicRule,
    Heuristic,
    LlmEstimate
}

public sealed record Availability(AvailabilityStatus Status, string? Reason = null);

public sealed record AnalyticsEnvelope<T>(
    ExplanationKind ExplanationType,
    ExplanationSource DecisionSource,
    Availability Availability,
    T? Data,
    string ExplanationVersion,
    DateTimeOffset GeneratedAt,
    Guid? AuditId = null);

public sealed record PercentileValue(double Percentile, double Value);
public sealed record HistogramBucket(double LowerBound, double UpperBound, int Count, bool IncludesUpperBound);
public sealed record ConfidenceInterval(double Level, double LowerBound, double UpperBound, string Method);
public sealed record OutlierIndicator(int SourceIndex, double Value, string Direction);

public sealed record StatisticalSummary(
    int Count,
    int ValidCount,
    int MissingCount,
    bool IsSufficient,
    string? InsufficiencyReason,
    double? Minimum,
    double? Maximum,
    double? Range,
    double? Mean,
    double? Median,
    IReadOnlyList<double> Modes,
    double? SampleVariance,
    double? SampleStandardDeviation,
    double? FirstQuartile,
    double? ThirdQuartile,
    double? InterquartileRange,
    IReadOnlyList<PercentileValue> Percentiles,
    double? Skewness,
    IReadOnlyList<HistogramBucket> Distribution,
    IReadOnlyList<OutlierIndicator> Outliers,
    ConfidenceInterval? MeanConfidenceInterval);

public sealed record CohortComparison(
    bool Suppressed,
    string? SuppressionReason,
    int SampleSize,
    double? CohortMean,
    double? CohortMedian,
    double? StandardDeviation,
    double? PercentileRank,
    double? ZScore);

public sealed record PeriodComparison(
    double? CurrentValue,
    double? PreviousValue,
    double? AbsoluteDelta,
    double? PercentageDelta,
    TrendDirection Direction);

public sealed record CorrelationResult(
    int PairCount,
    bool IsSufficient,
    double? PearsonCorrelation,
    string? Reason);

public sealed record AnovaResult(
    int GroupCount,
    int SampleSize,
    bool IsSufficient,
    double? FStatistic,
    int? DegreesOfFreedomBetween,
    int? DegreesOfFreedomWithin,
    double? EtaSquared,
    string? Reason);

public interface IStatisticalAnalyticsEngine
{
    StatisticalSummary Summarize(
        IReadOnlyList<double?> values,
        IReadOnlyList<double>? percentiles = null,
        int histogramBins = 10,
        int minimumValidCount = 2,
        double confidenceLevel = 0.95);

    double? PercentileRank(double value, IReadOnlyList<double?> population);
    double? ZScore(double value, IReadOnlyList<double?> population);
    CohortComparison CompareToCohort(double value, IReadOnlyList<double?> cohort, int minimumCohortSize);
    PeriodComparison ComparePeriods(double? current, double? previous, double flatTolerance = 0.001);
    IReadOnlyList<double?> RollingMean(IReadOnlyList<double?> values, int windowSize, int minimumValidValues = 1);
    CorrelationResult Correlate(IReadOnlyList<double?> left, IReadOnlyList<double?> right);
    AnovaResult OneWayAnova(IReadOnlyDictionary<string, IReadOnlyList<double?>> groups);
}

public sealed record TelemetryClassificationPolicy(
    double ExceptionalMinimum = 90,
    double StrongMinimum = 75,
    double DevelopingMinimum = 60,
    double ImprovementMinimumDelta = 2,
    double DeclineMinimumDelta = 2,
    double StableTolerance = 0.5);

public sealed record TelemetryClassification(
    PerformanceStatus AbsolutePerformance,
    ImprovementStatus PersonalProgress,
    TrendDirection TrendDirection);

public interface ITelemetryPolicyEngine
{
    TelemetryClassification Classify(
        double? normalizedValue,
        double? previousValue,
        IReadOnlyList<double?> personalHistory,
        TelemetryClassificationPolicy policy);
}

public sealed record TelemetryMetric(
    string Dimension,
    decimal? RawValue,
    decimal? NormalizedValue,
    decimal? Percentile,
    decimal? CohortMean,
    decimal? CohortMedian,
    decimal? StandardDeviation,
    decimal? ZScore,
    decimal? PersonalPreviousValue,
    decimal? PersonalDelta,
    decimal? CohortDelta,
    TrendDirection TrendDirection,
    PerformanceStatus PerformanceStatus,
    ImprovementStatus ImprovementStatus,
    int SampleSize,
    string Period,
    string ComparisonPopulation,
    DateTimeOffset FreshnessTimestamp,
    Availability Availability);

public sealed record StudentTelemetry(
    Guid StudentId,
    Guid? CourseId,
    string Period,
    string ComparisonPopulation,
    int MinimumCohortSize,
    IReadOnlyList<TelemetryMetric> Metrics,
    DateTimeOffset DataCutoffTimestamp,
    DateTimeOffset FreshnessTimestamp);

public sealed record GradeComponentExplanation(
    Guid AssessmentComponentId,
    string ComponentType,
    decimal RawScore,
    decimal MaximumScore,
    decimal NormalizedScore,
    decimal Weight,
    decimal WeightedContribution);

public sealed record GradeBoundaryExplanation(
    string Grade,
    decimal MinimumInclusive,
    decimal MaximumInclusive,
    decimal GpaPoints);

public sealed record GradeStatisticalContext(
    int SampleSize,
    decimal? CohortMean,
    decimal? CohortMedian,
    decimal? StandardDeviation,
    decimal? Percentile,
    decimal? ZScore,
    IReadOnlyList<HistogramBucket> Distribution,
    Availability Availability);

public sealed record GradeExplanation(
    Guid CourseGradeId,
    Guid StudentId,
    Guid CourseId,
    Guid SemesterId,
    decimal FinalScore,
    string FinalGrade,
    string GradingPolicyVersion,
    IReadOnlyList<GradeComponentExplanation> AssessmentComponents,
    decimal TotalWeightedScore,
    GradeBoundaryExplanation AppliedGradeBoundary,
    GradeBoundaryExplanation? NextGradeBoundary,
    decimal? DistanceToNextGrade,
    string RoundingPolicy,
    string? ModerationOrAdjustment,
    GradeStatisticalContext StatisticalContext,
    DateTimeOffset DecisionTimestamp,
    DateTimeOffset SnapshotTimestamp);

public sealed record AdmissionRequirementExplanation(
    string Requirement,
    bool? Passed,
    string? Evidence,
    string? FailureReason);

public sealed record AdmissionMeritComponent(
    string Name,
    decimal? RawValue,
    decimal? NormalizedValue,
    decimal? Weight,
    decimal Contribution);

public sealed record AdmissionDecisionExplanation(
    Guid ApplicationId,
    Guid MeritListId,
    string Decision,
    IReadOnlyList<AdmissionRequirementExplanation> EligibilityRequirements,
    IReadOnlyList<AdmissionMeritComponent> MeritComponents,
    decimal AggregateScore,
    int Rank,
    int SeatsAvailable,
    decimal? ClosingMerit,
    decimal? DistanceFromClosingMerit,
    int? WaitlistPosition,
    string? FailedPrerequisiteOrReason,
    string? PolicyVersion,
    DateTimeOffset DecisionTimestamp,
    Guid AuditReference,
    IReadOnlyList<string> Limitations);

public sealed record HumanRubricCriterion(
    string Criterion,
    decimal? Score,
    decimal? MaximumScore,
    decimal? Weight,
    string? RecordedReason);

public sealed record HumanDecisionEvidence(
    string EvidenceType,
    string Reference,
    DateTimeOffset RecordedAt);

public sealed record HumanDecisionExplanation(
    Guid DecisionId,
    string SubjectType,
    Guid SubjectId,
    string Decision,
    string RubricVersion,
    IReadOnlyList<HumanRubricCriterion> Rubric,
    IReadOnlyList<string> ReasonCodes,
    string? RecordedReason,
    IReadOnlyList<HumanDecisionEvidence> AuditEvidence,
    Guid AuditReference,
    DateTimeOffset DecisionTimestamp,
    string ExplanationVersion);

public sealed record ModelFeatureContribution(
    string FeatureName,
    decimal FeatureValue,
    decimal ShapValue,
    int Rank);

public sealed record PredictionExplanation(
    Guid PredictionId,
    Guid SubjectId,
    string SubjectType,
    ModelKind ModelKind,
    string ModelName,
    string ModelVersion,
    string FeatureSchemaVersion,
    DateTimeOffset PredictionTimestamp,
    DateTimeOffset DataCutoffTimestamp,
    decimal RawPrediction,
    decimal? CalibratedProbability,
    decimal BaselineExpectedValue,
    IReadOnlyDictionary<string, decimal> FeatureValues,
    IReadOnlyDictionary<string, decimal> ShapValues,
    IReadOnlyList<ModelFeatureContribution> RankedLocalContributions,
    string ExplanationAlgorithm,
    string ExplanationAlgorithmVersion,
    string Label,
    bool RequiresHumanReview,
    string Disclaimer);

public sealed record ModelQuality(
    string ModelName,
    string ModelVersion,
    DateTimeOffset? TrainingPeriodStart,
    DateTimeOffset? TrainingPeriodEnd,
    int? TrainingSampleSize,
    decimal? CalibrationError,
    decimal? AuRoc,
    decimal? Precision,
    decimal? Recall,
    decimal? Specificity,
    decimal? F1,
    int? TruePositive,
    int? TrueNegative,
    int? FalsePositive,
    int? FalseNegative,
    decimal? Threshold,
    decimal? DriftIndicator,
    DateTimeOffset EvaluationTimestamp,
    IReadOnlyList<string> SensitiveOrProxyFeatureReview,
    IReadOnlyDictionary<string, decimal> FairnessMonitoringMetrics,
    string EvidenceStatement);

public sealed record GlobalModelExplanation(
    Guid SnapshotId,
    string ModelName,
    string ModelVersion,
    string FeatureSchemaVersion,
    string AuthorizedPopulation,
    int SampleSize,
    DateTimeOffset PopulationStart,
    DateTimeOffset PopulationEnd,
    IReadOnlyDictionary<string, decimal> MeanAbsoluteShapValues,
    string ExplanationAlgorithm,
    string ExplanationAlgorithmVersion,
    DateTimeOffset CalculatedAt,
    string Disclaimer);

public sealed record PredictionSnapshotWrite(
    Guid PredictionId,
    Guid SubjectId,
    string SubjectType,
    ModelKind ModelKind,
    string ModelName,
    string ModelVersion,
    string FeatureSchemaVersion,
    DateTimeOffset PredictionTimestamp,
    DateTimeOffset DataCutoffTimestamp,
    decimal RawPrediction,
    decimal? CalibratedProbability,
    decimal BaselineExpectedValue,
    IReadOnlyDictionary<string, decimal> FeatureValues,
    IReadOnlyDictionary<string, decimal> ShapValues,
    string ExplanationAlgorithm,
    string ExplanationAlgorithmVersion,
    bool RequiresHumanReview);

public interface IAnalyticsIntelligenceService
{
    Task<StudentTelemetry?> GetLatestTelemetryAsync(Guid studentId, Guid? courseId, CancellationToken ct = default);
    Task<StudentTelemetry?> GetAuthorizedTelemetryAsync(Guid studentId, Guid? courseId, Guid? departmentScopeId, bool tenantWide, CancellationToken ct = default);
    Task<AnalyticsEnvelope<GradeExplanation>?> GetGradeExplanationAsync(Guid courseGradeId, Guid studentId, CancellationToken ct = default);
    Task<AnalyticsEnvelope<AdmissionDecisionExplanation>?> GetAdmissionExplanationAsync(Guid applicationId, CancellationToken ct = default);
    Task<AnalyticsEnvelope<PredictionExplanation>?> GetPredictionExplanationAsync(Guid predictionId, Guid? studentId, Guid? departmentScopeId, bool tenantWide, CancellationToken ct = default);
    Task<ModelQuality?> GetLatestModelQualityAsync(string modelName, string? modelVersion, CancellationToken ct = default);
    Task<GlobalModelExplanation?> GetLatestGlobalExplanationAsync(string modelName, string? modelVersion, CancellationToken ct = default);
    Task<Guid> PersistPredictionSnapshotAsync(PredictionSnapshotWrite snapshot, CancellationToken ct = default);
    Task<Guid> PersistModelQualitySnapshotAsync(ModelQuality quality, string evaluationDataHash, CancellationToken ct = default);
    Task<Guid> PersistGlobalExplanationSnapshotAsync(GlobalModelExplanation explanation, string populationHash, ModelKind modelKind, CancellationToken ct = default);
    Task<Guid> PersistTelemetrySnapshotAsync(StudentTelemetry telemetry, CancellationToken ct = default);
}
