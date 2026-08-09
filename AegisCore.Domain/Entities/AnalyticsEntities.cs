using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("AnalyticsDecisionSnapshots")]
public sealed class AnalyticsDecisionSnapshot : ITenantScoped, IImmutableAnalyticsSnapshot
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    [Required, MaxLength(50)] public string ExplanationType { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    [Required, MaxLength(20)] public string DecisionSource { get; set; } = string.Empty;
    [MaxLength(128)] public string? PolicyVersion { get; set; }
    [Required, MaxLength(30)] public string ExplanationVersion { get; set; } = "1.0";
    [Required, MaxLength(64)] public string InputHash { get; set; } = string.Empty;
    [Required, Column(TypeName = "jsonb")] public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset DataCutoffAt { get; set; }
    public DateTimeOffset CalculatedAt { get; set; }
    public Guid AuditId { get; set; }
}

[Table("AnalyticsStudentTelemetrySnapshots")]
public sealed class AnalyticsStudentTelemetrySnapshot : ITenantScoped, IImmutableAnalyticsSnapshot
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? CourseId { get; set; }
    [Required, MaxLength(80)] public string Period { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string ComparisonPopulation { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string InputHash { get; set; } = string.Empty;
    [Required, Column(TypeName = "jsonb")] public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset DataCutoffAt { get; set; }
    public DateTimeOffset FreshnessAt { get; set; }
}

[Table("AnalyticsModelPredictionSnapshots")]
public sealed class AnalyticsModelPredictionSnapshot : ITenantScoped, IImmutableAnalyticsSnapshot
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SubjectId { get; set; }
    [Required, MaxLength(50)] public string SubjectType { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string ModelKind { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string ModelName { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string ModelVersion { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string FeatureSchemaVersion { get; set; } = string.Empty;
    public DateTimeOffset PredictionTimestamp { get; set; }
    public DateTimeOffset DataCutoffTimestamp { get; set; }
    [Column(TypeName = "numeric(20,8)")] public decimal RawPrediction { get; set; }
    [Column(TypeName = "numeric(20,8)")] public decimal? CalibratedProbability { get; set; }
    [Column(TypeName = "numeric(20,8)")] public decimal BaselineExpectedValue { get; set; }
    [Required, Column(TypeName = "jsonb")] public string FeatureValuesJson { get; set; } = "{}";
    [Required, Column(TypeName = "jsonb")] public string ShapValuesJson { get; set; } = "{}";
    [Required, MaxLength(80)] public string ExplanationAlgorithm { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string ExplanationAlgorithmVersion { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string InputHash { get; set; } = string.Empty;
    public bool RequiresHumanReview { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

[Table("AnalyticsModelQualitySnapshots")]
public sealed class AnalyticsModelQualitySnapshot : ITenantScoped, IImmutableAnalyticsSnapshot
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    [Required, MaxLength(160)] public string ModelName { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string ModelVersion { get; set; } = string.Empty;
    [Required, Column(TypeName = "jsonb")] public string PayloadJson { get; set; } = "{}";
    [Required, MaxLength(64)] public string EvaluationDataHash { get; set; } = string.Empty;
    public DateTimeOffset EvaluationTimestamp { get; set; }
}

[Table("AnalyticsGlobalExplanationSnapshots")]
public sealed class AnalyticsGlobalExplanationSnapshot : ITenantScoped, IImmutableAnalyticsSnapshot
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    [Required, MaxLength(160)] public string ModelName { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string ModelVersion { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string FeatureSchemaVersion { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string AuthorizedPopulation { get; set; } = string.Empty;
    public int SampleSize { get; set; }
    public DateTimeOffset PopulationStart { get; set; }
    public DateTimeOffset PopulationEnd { get; set; }
    [Required, Column(TypeName = "jsonb")] public string MeanAbsoluteShapValuesJson { get; set; } = "{}";
    [Required, MaxLength(80)] public string ExplanationAlgorithm { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string ExplanationAlgorithmVersion { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string PopulationHash { get; set; } = string.Empty;
    public DateTimeOffset CalculatedAt { get; set; }
}
