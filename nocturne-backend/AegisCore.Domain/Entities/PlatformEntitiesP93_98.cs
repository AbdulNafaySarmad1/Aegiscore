using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// P93 Performance Benchmark
[Table("BenchmarkRuns")] public class BenchmarkRun { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string ScenarioName { get; set; } = ""; public BenchmarkTier Tier { get; set; }
    public BenchmarkStatus Status { get; set; } = BenchmarkStatus.Running;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow; public DateTime? CompletedAt { get; set; }
    public double? RequestsPerSecond { get; set; } public double? P95LatencyMs { get; set; }
    public double? ErrorRate { get; set; } [Column(TypeName = "jsonb")] public string ResultsJson { get; set; } = "{}";
    public Guid TenantId { get; set; } }

// P94 AI Operations Center
[Table("AIProviders")] public class AIProvider { [Key] public Guid Id { get; set; }
    [Required][MaxLength(100)] public string Name { get; set; } = ""; public AIProviderType ProviderType { get; set; }
    [MaxLength(500)] public string? BaseUrl { get; set; } [MaxLength(200)] public string? DefaultModel { get; set; }
    public bool IsActive { get; set; } = true; public bool IsFallbackOnly { get; set; }
    public int TimeoutSeconds { get; set; } = 60; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("AIModels")] public class AIModel { [Key] public Guid Id { get; set; }
    public Guid ProviderId { get; set; } [Required][MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(50)] public string? Version { get; set; } public int ContextWindow { get; set; }
    [Column(TypeName = "decimal(12,6)")] public decimal CostPerInputToken { get; set; }
    [Column(TypeName = "decimal(12,6)")] public decimal CostPerOutputToken { get; set; }
    [MaxLength(500)] public string? Capabilities { get; set; } public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("AITokenUsage")] public class AITokenUsage { [Key] public Guid Id { get; set; }
    public Guid ModelId { get; set; } [MaxLength(100)] public string Domain { get; set; } = "";
    public int InputTokens { get; set; } public int OutputTokens { get; set; }
    public int LatencyMs { get; set; } public bool IsSuccess { get; set; } = true;
    [MaxLength(2000)] public string? ErrorMessage { get; set; } public DateTime RecordedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P95 Prompt Management
[Table("Prompts")] public class Prompt { [Key] public Guid Id { get; set; }
    [Required][MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(100)] public string Domain { get; set; } = "";
    [MaxLength(100)] public string? ModelTarget { get; set; } public int CurrentVersion { get; set; } = 1;
    public PromptApprovalStatus ApprovalStatus { get; set; } = PromptApprovalStatus.Draft;
    public int ActiveVersion { get; set; }
    [Required][Column(TypeName = "text")] public string SystemPrompt { get; set; } = "";
    [Required][Column(TypeName = "text")] public string UserPromptTemplate { get; set; } = "";
    [Column(TypeName = "jsonb")] public string VariablesJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string ModelParametersJson { get; set; } = "{}";
    public Guid CreatedByUserId { get; set; } public Guid? ReviewedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("PromptVersions")] public class PromptVersion { [Key] public Guid Id { get; set; }
    public Guid PromptId { get; set; } public int VersionNumber { get; set; }
    [Required][Column(TypeName = "text")] public string SystemPrompt { get; set; } = "";
    [Required][Column(TypeName = "text")] public string UserPromptTemplate { get; set; } = "";
    [MaxLength(500)] public string? ChangeDescription { get; set; } public Guid ChangedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

[Table("PromptABTests")] public class PromptABTest { [Key] public Guid Id { get; set; }
    public Guid PromptId { get; set; } public int VersionA { get; set; } public int VersionB { get; set; }
    public double TrafficSplitA { get; set; } = 0.5; public int ResponsesA { get; set; } public int ResponsesB { get; set; }
    public int ThumbsUpA { get; set; } public int ThumbsUpB { get; set; }
    public double AvgLatencyMsA { get; set; } public double AvgLatencyMsB { get; set; }
    public double AvgTokensA { get; set; } public double AvgTokensB { get; set; }
    public bool IsActive { get; set; } public Guid TenantId { get; set; } }

// P96 AI Guardrails
[Table("GuardrailRules")] public class GuardrailRule { [Key] public Guid Id { get; set; }
    [Required][MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(100)] public string Domain { get; set; } = "";
    public GuardrailAction Action { get; set; } = GuardrailAction.Block;
    public GuardrailSeverity Severity { get; set; } = GuardrailSeverity.Medium;
    [MaxLength(1000)] public string? Pattern { get; set; } [MaxLength(100)] public string? Category { get; set; }
    public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("GuardrailViolations")] public class GuardrailViolation { [Key] public Guid Id { get; set; }
    public Guid RuleId { get; set; } [MaxLength(100)] public string Domain { get; set; } = "";
    [MaxLength(5000)] public string? InputPreview { get; set; } [MaxLength(5000)] public string? OutputPreview { get; set; }
    public GuardrailAction ActionTaken { get; set; } public DateTime OccurredAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P97 AI Analytics
[Table("AIFeedback")] public class AIFeedback { [Key] public Guid Id { get; set; }
    [MaxLength(100)] public string Domain { get; set; } = ""; public Guid? PromptId { get; set; }
    public int Rating { get; set; } public bool IsThumbsUp { get; set; }
    [MaxLength(2000)] public string? Comment { get; set; } public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P98 License Management
[Table("Licenses")] public class License { [Key] public Guid Id { get; set; }
    [Required][MaxLength(200)] public string CustomerName { get; set; } = ""; [MaxLength(300)] public string? CustomerEmail { get; set; }
    [MaxLength(100)] public string LicenseKey { get; set; } = ""; public LicenseEdition Edition { get; set; }
    public LicenseStatus Status { get; set; } = LicenseStatus.Active;
    public int MaxUsers { get; set; } public int MaxCampuses { get; set; }
    [Column(TypeName = "jsonb")] public string FeatureEntitlementsJson { get; set; } = "{}";
    public DateTime IssuedAt { get; set; } public DateTime? ExpiresAt { get; set; } public DateTime? RevokedAt { get; set; }
    [MaxLength(500)] public string? RevocationReason { get; set; } public Guid TenantId { get; set; } }

[Table("LicenseHeartbeats")] public class LicenseHeartbeat { [Key] public Guid Id { get; set; }
    public Guid LicenseId { get; set; } [MaxLength(100)] public string InstanceId { get; set; } = "";
    [MaxLength(50)] public string? Version { get; set; } public int ActiveUsers { get; set; }
    public DateTime HeartbeatAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }
