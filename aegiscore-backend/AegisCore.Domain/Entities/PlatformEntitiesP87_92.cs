using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// P87 Queue Management
[Table("QueueDefinitions")] public class QueueDefinition { [Key] public Guid Id { get; set; }
    [Required][MaxLength(100)] public string Name { get; set; } = ""; [MaxLength(500)] public string? Purpose { get; set; }
    public int CurrentDepth { get; set; } public int ConsumerCount { get; set; } public QueueStatus Status { get; set; } = QueueStatus.Healthy;
    public int JobsPerMinute { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("QueueJobs")] public class QueueJob { [Key] public Guid Id { get; set; }
    public Guid QueueId { get; set; } [MaxLength(200)] public string JobType { get; set; } = "";
    [Column(TypeName = "jsonb")] public string PayloadJson { get; set; } = "{}";
    public JobExecutionStatus Status { get; set; } = JobExecutionStatus.Queued;
    public int Priority { get; set; } public int AttemptCount { get; set; } public DateTime? EnqueuedAt { get; set; }
    public DateTime? StartedAt { get; set; } public DateTime? CompletedAt { get; set; }
    [MaxLength(5000)] public string? ErrorMessage { get; set; } public Guid TenantId { get; set; } }

// P88 Event Explorer
[Table("DomainEvents")] public class DomainEvent { [Key] public Guid Id { get; set; }
    [Required][MaxLength(200)] public string EventType { get; set; } = ""; [MaxLength(100)] public string AggregateId { get; set; } = "";
    [MaxLength(100)] public string? CorrelationId { get; set; } [MaxLength(100)] public string? CausationId { get; set; }
    [MaxLength(100)] public string SourceModule { get; set; } = "";
    [Column(TypeName = "jsonb")] public string PayloadJson { get; set; } = "{}";
    public EventStatus Status { get; set; } = EventStatus.Published; public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; } public Guid TenantId { get; set; } }

[Table("EventSchemaVersions")] public class EventSchemaVersion { [Key] public Guid Id { get; set; }
    [Required][MaxLength(200)] public string EventType { get; set; } = ""; public int Version { get; set; } = 1;
    [Column(TypeName = "jsonb")] public string SchemaJson { get; set; } = "{}"; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

// P89 Diagnostics Center
[Table("DiagnosticFindings")] public class DiagnosticFinding { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = ""; [MaxLength(100)] public string Category { get; set; } = "";
    public DiagnosticSeverity Severity { get; set; } = DiagnosticSeverity.Info;
    [MaxLength(2000)] public string? Description { get; set; } [MaxLength(1000)] public string? Recommendation { get; set; }
    [Column(TypeName = "jsonb")] public string? DataJson { get; set; } public DateTime FoundAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("SlowQueries")] public class SlowQuery { [Key] public Guid Id { get; set; }
    [MaxLength(5000)] public string QueryText { get; set; } = ""; public double DurationMs { get; set; }
    [MaxLength(100)] public string? SourceModule { get; set; } public int Frequency { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P90 Architecture Explorer
[Table("ServiceRegistrations")] public class ServiceRegistration { [Key] public Guid Id { get; set; }
    [Required][MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(100)] public string? Runtime { get; set; }
    public int Port { get; set; } public ServiceStatus Status { get; set; } = ServiceStatus.Up;
    [MaxLength(500)] public string? HealthEndpoint { get; set; } public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastHeartbeatAt { get; set; } public Guid TenantId { get; set; } }

[Table("ServiceDependencies")] public class ServiceDependency { [Key] public Guid Id { get; set; }
    public Guid SourceServiceId { get; set; } public Guid TargetServiceId { get; set; }
    public DependencyType DependencyType { get; set; } = DependencyType.API;
    [MaxLength(500)] public string? Description { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

[Table("ApiEndpoints")] public class ApiEndpoint { [Key] public Guid Id { get; set; }
    [MaxLength(20)] public string Method { get; set; } = "GET"; [MaxLength(500)] public string Path { get; set; } = "";
    [MaxLength(100)] public string? Module { get; set; } public bool AuthRequired { get; set; }
    [MaxLength(50)] public string? Version { get; set; } public DateTime RegisteredAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P91 Health Analyzer
[Table("HealthScores")] public class HealthScore { [Key] public Guid Id { get; set; }
    public double OverallScore { get; set; } public double PerformanceScore { get; set; } public double SecurityScore { get; set; }
    public double AvailabilityScore { get; set; } public double ComplianceScore { get; set; }
    public double StorageScore { get; set; } public double DatabaseScore { get; set; } public double IntegrationScore { get; set; }
    public HealthScoreGrade Grade { get; set; } [Column(TypeName = "jsonb")] public string TopIssuesJson { get; set; } = "[]";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// P92 Integration Monitoring
[Table("IntegrationRegistrations")] public class IntegrationRegistration { [Key] public Guid Id { get; set; }
    [Required][MaxLength(200)] public string Name { get; set; } = ""; public IntegrationType Type { get; set; }
    [MaxLength(500)] public string? Endpoint { get; set; } public IntegrationStatus Status { get; set; } = IntegrationStatus.Up;
    public int CheckIntervalSeconds { get; set; } = 300; public DateTime? LastCheckedAt { get; set; }
    public DateTime? LastSuccessAt { get; set; } public DateTime? LastFailureAt { get; set; }
    public double UptimePercentage { get; set; } [MaxLength(2000)] public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("IntegrationIncidents")] public class IntegrationIncident { [Key] public Guid Id { get; set; }
    public Guid IntegrationId { get; set; } public DateTime StartedAt { get; set; } public DateTime? ResolvedAt { get; set; }
    public int DurationMinutes { get; set; } [MaxLength(2000)] public string? Resolution { get; set; } public Guid TenantId { get; set; } }
