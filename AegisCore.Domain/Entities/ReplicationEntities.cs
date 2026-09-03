using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("ReplicationScalingConfigs")]
public class ReplicationScalingConfig : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public int MinReplicas { get; set; } = 1;
    public int MaxReplicas { get; set; } = 10;
    public int TargetReplicas { get; set; } = 3;
    public int ScaleUpThresholdConnections { get; set; } = 200;
    public int ScaleUpThresholdQueriesPerSecond { get; set; } = 500;
    public int ScaleDownThresholdConnections { get; set; } = 50;
    public int ScaleDownCooldownMinutes { get; set; } = 30;
    public int DefaultMaxLagSeconds { get; set; } = 30;
    public int CriticalLagSeconds { get; set; } = 120;
    public bool AutoScalingEnabled { get; set; } = true;
    public bool AutoFailoverEnabled { get; set; } = false;
    public bool ReadRoutingEnabled { get; set; } = true;
    [MaxLength(50)] public string? ReadDistributionStrategy { get; set; } = "round-robin"; // round-robin | least-loaded | weighted
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
    public Guid TenantId { get; set; }
}

[Table("ReplicaHealthLogs")]
public class ReplicaHealthLog : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ReplicaId { get; set; }
    public bool IsHealthy { get; set; }
    public int? LagSeconds { get; set; }
    public int? ActiveConnections { get; set; }
    public double? QueriesPerSecond { get; set; }
    public bool LagThresholdBreached { get; set; }
    [MaxLength(2000)] public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("ReplicationScaleEvents")]
public class ReplicationScaleEvent : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [MaxLength(20)] public string Action { get; set; } = ""; // scale-up | scale-down | failover
    public int PreviousReplicaCount { get; set; }
    public int NewReplicaCount { get; set; }
    [MaxLength(200)] public string? TriggerReason { get; set; }
    [MaxLength(100)] public string? TriggeredBy { get; set; } // auto | manual
    public bool WasSuccessful { get; set; }
    [MaxLength(2000)] public string? ErrorMessage { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid TenantId { get; set; }
}

[Table("ReplicationLagAlerts")]
public class ReplicationLagAlert : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ReplicaId { get; set; }
    public int LagSeconds { get; set; }
    public int ThresholdSeconds { get; set; }
    public bool IsCritical { get; set; }
    [MaxLength(1000)] public string? Message { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
