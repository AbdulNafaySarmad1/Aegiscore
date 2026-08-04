using AegisCore.Domain.Entities;

namespace AegisCore.Application.Replication;

public interface IReplicaManagerService
{
    Task<ReadReplicaConfig> RegisterReplicaAsync(string name, string connectionString, int priority, int weight, CancellationToken ct);
    Task<bool> TestReplicaConnectionAsync(Guid replicaId, CancellationToken ct);
    Task CheckAllReplicaHealthAsync(CancellationToken ct);
    Task<int> CheckReplicaLagAsync(Guid replicaId, CancellationToken ct);
    Task AutoScaleAsync(CancellationToken ct);
    Task<bool> PromoteReplicaAsync(Guid replicaId, CancellationToken ct);
    Task<string> RouteReadConnectionAsync(CancellationToken ct);
    Task<bool> IsReplicaLagAcceptableAsync(Guid replicaId, CancellationToken ct);
    Task<ReplicationScalingConfig> GetScalingConfigAsync(CancellationToken ct);
    Task<ReplicationScalingConfig> UpdateScalingConfigAsync(ReplicationScalingConfig config, CancellationToken ct);
    Task<List<ReadReplicaConfig>> GetActiveReplicasAsync(CancellationToken ct);
    Task<List<ReplicationScaleEvent>> GetScaleHistoryAsync(int limit, CancellationToken ct);
    Task<List<ReplicationLagAlert>> GetActiveAlertsAsync(CancellationToken ct);
}

public class ReplicationDashboardDto
{
    public bool PrimaryHealthy { get; set; }
    public bool AutoScalingEnabled { get; set; }
    public bool ReadRoutingEnabled { get; set; }
    public int OnlineReplicas { get; set; }
    public int TotalReplicas { get; set; }
    public int MaxReplicas { get; set; }
    public int MaxLagSeconds { get; set; }
    public int CriticalLagSeconds { get; set; }
    public int ActiveConnections { get; set; }
    public double AvgQueriesPerSecond { get; set; }
    public int ActiveAlertsCount { get; set; }
    public List<ReplicaHealthDto> Replicas { get; set; } = new();
}

public class ReplicaHealthDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int Priority { get; set; }
    public int Weight { get; set; }
    public int? CurrentLagSeconds { get; set; }
    public int MaxLagSeconds { get; set; }
    public bool IsLagAcceptable { get; set; }
    public int? ActiveConnections { get; set; }
    public double? QueriesPerSecond { get; set; }
    public DateTime? LastHealthCheckAt { get; set; }
}
