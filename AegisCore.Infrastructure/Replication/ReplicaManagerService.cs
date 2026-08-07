using Npgsql;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Replication;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Replication;

public class ReplicaManagerService : IReplicaManagerService
{
    private readonly IRepository<ReadReplicaConfig> _replicaRepo;
    private readonly IRepository<ReplicationScalingConfig> _configRepo;
    private readonly IRepository<ReplicaHealthLog> _healthLogRepo;
    private readonly IRepository<ReplicationScaleEvent> _scaleEventRepo;
    private readonly IRepository<ReplicationLagAlert> _alertRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<ReplicaManagerService> _logger;
    private static int _roundRobinIndex;

    public ReplicaManagerService(
        IRepository<ReadReplicaConfig> replicaRepo,
        IRepository<ReplicationScalingConfig> configRepo,
        IRepository<ReplicaHealthLog> healthLogRepo,
        IRepository<ReplicationScaleEvent> scaleEventRepo,
        IRepository<ReplicationLagAlert> alertRepo,
        IConfiguration config,
        ILogger<ReplicaManagerService> logger)
    {
        _replicaRepo = replicaRepo; _configRepo = configRepo;
        _healthLogRepo = healthLogRepo; _scaleEventRepo = scaleEventRepo;
        _alertRepo = alertRepo; _config = config; _logger = logger;
    }

    private string PrimaryConnectionString()
        => _config["AEGISCORE_DB_CONNECTION"] ?? _config.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Primary database connection string not configured.");

    public async Task<ReadReplicaConfig> RegisterReplicaAsync(string name, string connectionString, int priority, int weight, CancellationToken ct)
    {
        var replica = new ReadReplicaConfig { Id = Guid.NewGuid(), Name = name, ConnectionString = connectionString, Priority = priority, Weight = weight, Status = ReplicaStatus.Online, TenantId = Guid.Empty };
        await _replicaRepo.AddAsync(replica, ct);
        _logger.LogInformation("Replica registered: {Name} (priority {Priority}, weight {Weight})", name, priority, weight);
        return replica;
    }

    public async Task<bool> TestReplicaConnectionAsync(Guid replicaId, CancellationToken ct)
    {
        var replica = await _replicaRepo.GetByIdAsync(replicaId, ct);
        if (replica == null) return false;
        try
        {
            await using var conn = new NpgsqlConnection(replica.ConnectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(ct);
            replica.LastHealthCheckAt = DateTime.UtcNow;
            await _replicaRepo.UpdateAsync(replica, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Replica {Name} connection test failed", replica.Name);
            return false;
        }
    }

    public async Task CheckAllReplicaHealthAsync(CancellationToken ct)
    {
        var replicas = await _replicaRepo.FindAsync(r => r.IsEnabled, ct);
        foreach (var replica in replicas)
        {
            var healthy = await TestReplicaConnectionAsync(replica.Id, ct);
            var lag = healthy ? await QueryReplicaLagSecondsAsync(replica.ConnectionString, ct) : (int?)null;
            var lagBreached = lag.HasValue && lag.Value > replica.MaxLagSeconds;

            replica.CurrentLagSeconds = lag;
            replica.Status = healthy ? ReplicaStatus.Online : ReplicaStatus.Offline;
            replica.LastHealthCheckAt = DateTime.UtcNow;
            await _replicaRepo.UpdateAsync(replica, ct);

            await _healthLogRepo.AddAsync(new ReplicaHealthLog { Id = Guid.NewGuid(), ReplicaId = replica.Id, IsHealthy = healthy, LagSeconds = lag, LagThresholdBreached = lagBreached, TenantId = Guid.Empty }, ct);

            if (lagBreached)
            {
                var scaling = await GetScalingConfigAsync(ct);
                var isCritical = lag.Value > scaling.CriticalLagSeconds;
                await _alertRepo.AddAsync(new ReplicationLagAlert { Id = Guid.NewGuid(), ReplicaId = replica.Id, LagSeconds = lag.Value, ThresholdSeconds = replica.MaxLagSeconds, IsCritical = isCritical, Message = $"Replica {replica.Name} lag is {lag}s (threshold: {replica.MaxLagSeconds}s)" }, ct);
                if (isCritical) _logger.LogError("CRITICAL: Replica {Name} lag {Lag}s exceeds critical threshold {Threshold}s", replica.Name, lag, scaling.CriticalLagSeconds);
                else _logger.LogWarning("Replica {Name} lag {Lag}s exceeds threshold {Threshold}s", replica.Name, lag, replica.MaxLagSeconds);
            }
        }
        _logger.LogInformation("Health check complete: {Online}/{Total} replicas healthy", replicas.Count(r => r.Status == ReplicaStatus.Online), replicas.Count);
    }

    public async Task<int> CheckReplicaLagAsync(Guid replicaId, CancellationToken ct)
    {
        var replica = await _replicaRepo.GetByIdAsync(replicaId, ct);
        if (replica == null) return -1;
        var lag = await QueryReplicaLagSecondsAsync(replica.ConnectionString, ct);
        replica.CurrentLagSeconds = lag;
        await _replicaRepo.UpdateAsync(replica, ct);
        return lag;
    }

    private async Task<int> QueryReplicaLagSecondsAsync(string connectionString, CancellationToken ct)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT COALESCE(EXTRACT(EPOCH FROM (now() - pg_last_xact_replay_timestamp()))::int, 0)", conn);
            var result = await cmd.ExecuteScalarAsync(ct);
            return result is long l ? (int)l : result is int i ? i : 0;
        }
        catch { return -1; }
    }

    public async Task AutoScaleAsync(CancellationToken ct)
    {
        var scaling = await GetScalingConfigAsync(ct);
        if (!scaling.AutoScalingEnabled) return;

        var activeReplicas = await _replicaRepo.FindAsync(r => r.IsEnabled && r.Status == ReplicaStatus.Online, ct);
        var current = activeReplicas.Count;
        var totalConnections = 0; var totalQps = 0.0;
        foreach (var r in activeReplicas)
        {
            await using var conn = new NpgsqlConnection(r.ConnectionString);
            try
            {
                await conn.OpenAsync(ct);
                await using var cmd = new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE state = 'active'", conn);
                var count = await cmd.ExecuteScalarAsync(ct);
                if (count is long l) totalConnections += (int)l;
                else if (count is int i) totalConnections += i;
            }
            catch { }
        }
        totalQps = totalConnections * 0.8;

        if (totalConnections > scaling.ScaleUpThresholdConnections && current < scaling.MaxReplicas)
        {
            var target = Math.Min(current + 2, scaling.MaxReplicas);
            _logger.LogInformation("AUTO-SCALE UP: {Current}→{Target} replicas (connections: {Conn}, threshold: {Thresh})", current, target, totalConnections, scaling.ScaleUpThresholdConnections);
            await _scaleEventRepo.AddAsync(new ReplicationScaleEvent { Id = Guid.NewGuid(), Action = "scale-up", PreviousReplicaCount = current, NewReplicaCount = target, TriggerReason = $"Connections {totalConnections} > threshold {scaling.ScaleUpThresholdConnections}", TriggeredBy = "auto", WasSuccessful = false }, ct);
        }
        else if (totalConnections < scaling.ScaleDownThresholdConnections && current > scaling.MinReplicas)
        {
            var recentScale = await _scaleEventRepo.FindAsync(e => e.Action == "scale-down", ct);
            var lastScaleDown = recentScale.OrderByDescending(e => e.TriggeredAt).FirstOrDefault();
            if (lastScaleDown == null || (DateTime.UtcNow - lastScaleDown.TriggeredAt).TotalMinutes > scaling.ScaleDownCooldownMinutes)
            {
                var target = Math.Max(current - 1, scaling.MinReplicas);
                _logger.LogInformation("AUTO-SCALE DOWN: {Current}→{Target} replicas (connections: {Conn}, threshold: {Thresh})", current, target, totalConnections, scaling.ScaleDownThresholdConnections);
                await _scaleEventRepo.AddAsync(new ReplicationScaleEvent { Id = Guid.NewGuid(), Action = "scale-down", PreviousReplicaCount = current, NewReplicaCount = target, TriggerReason = $"Connections {totalConnections} < threshold {scaling.ScaleDownThresholdConnections}", TriggeredBy = "auto", WasSuccessful = false }, ct);
            }
        }
    }

    public async Task<bool> PromoteReplicaAsync(Guid replicaId, CancellationToken ct)
    {
        var replica = await _replicaRepo.GetByIdAsync(replicaId, ct);
        if (replica == null) return false;
        try
        {
            await using var conn = new NpgsqlConnection(replica.ConnectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT pg_promote()", conn);
            await cmd.ExecuteNonQueryAsync(ct);
            replica.Status = ReplicaStatus.Promoting;
            await _replicaRepo.UpdateAsync(replica, ct);
            await _scaleEventRepo.AddAsync(new ReplicationScaleEvent { Id = Guid.NewGuid(), Action = "failover", PreviousReplicaCount = 1, NewReplicaCount = 1, TriggerReason = $"Replica {replica.Name} promoted to primary", TriggeredBy = "auto", WasSuccessful = true, CompletedAt = DateTime.UtcNow }, ct);
            _logger.LogInformation("Replica {Name} promoted to primary", replica.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to promote replica {Name}", replica.Name);
            return false;
        }
    }

    public async Task<string> RouteReadConnectionAsync(CancellationToken ct)
    {
        var scaling = await GetScalingConfigAsync(ct);
        if (!scaling.ReadRoutingEnabled) return PrimaryConnectionString();

        var replicas = await _replicaRepo.FindAsync(r => r.IsEnabled && r.Status == ReplicaStatus.Online, ct);
        var available = replicas.Where(r => r.CurrentLagSeconds != null && r.CurrentLagSeconds <= r.MaxLagSeconds).ToList();
        if (available.Count == 0) return PrimaryConnectionString();

        return scaling.ReadDistributionStrategy switch
        {
            "round-robin" => RoundRobin(available),
            "weighted" => WeightedDistribution(available),
            "least-loaded" => LeastLoadedDistribution(available),
            _ => RoundRobin(available)
        };
    }

    private string RoundRobin(List<ReadReplicaConfig> replicas)
    {
        var idx = Interlocked.Increment(ref _roundRobinIndex) % replicas.Count;
        if (idx < 0) idx += replicas.Count;
        return replicas[idx].ConnectionString;
    }

    private string WeightedDistribution(List<ReadReplicaConfig> replicas)
    {
        var totalWeight = replicas.Sum(r => r.Weight);
        var random = Random.Shared.Next(totalWeight);
        var cumulative = 0;
        foreach (var r in replicas) { cumulative += r.Weight; if (random < cumulative) return r.ConnectionString; }
        return replicas.Last().ConnectionString;
    }

    private string LeastLoadedDistribution(List<ReadReplicaConfig> replicas)
        => replicas.OrderBy(r => r.CurrentLagSeconds ?? int.MaxValue).First().ConnectionString;

    public async Task<bool> IsReplicaLagAcceptableAsync(Guid replicaId, CancellationToken ct)
    {
        var replica = await _replicaRepo.GetByIdAsync(replicaId, ct);
        return replica != null && replica.CurrentLagSeconds <= replica.MaxLagSeconds;
    }

    public async Task<ReplicationScalingConfig> GetScalingConfigAsync(CancellationToken ct)
    {
        var configs = await _configRepo.GetAllAsync(ct);
        var config = configs.FirstOrDefault();
        if (config == null) { config = new ReplicationScalingConfig { Id = Guid.NewGuid(), TenantId = Guid.Empty }; await _configRepo.AddAsync(config, ct); }
        return config;
    }

    public async Task<ReplicationScalingConfig> UpdateScalingConfigAsync(ReplicationScalingConfig config, CancellationToken ct)
    {
        var existing = await GetScalingConfigAsync(ct);
        existing.MinReplicas = config.MinReplicas;
        existing.MaxReplicas = Math.Min(config.MaxReplicas, 10);
        existing.TargetReplicas = config.TargetReplicas;
        existing.ScaleUpThresholdConnections = config.ScaleUpThresholdConnections;
        existing.ScaleUpThresholdQueriesPerSecond = config.ScaleUpThresholdQueriesPerSecond;
        existing.ScaleDownThresholdConnections = config.ScaleDownThresholdConnections;
        existing.ScaleDownCooldownMinutes = config.ScaleDownCooldownMinutes;
        existing.DefaultMaxLagSeconds = config.DefaultMaxLagSeconds;
        existing.CriticalLagSeconds = config.CriticalLagSeconds;
        existing.AutoScalingEnabled = config.AutoScalingEnabled;
        existing.AutoFailoverEnabled = config.AutoFailoverEnabled;
        existing.ReadRoutingEnabled = config.ReadRoutingEnabled;
        existing.ReadDistributionStrategy = config.ReadDistributionStrategy;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedByUserId = config.UpdatedByUserId;
        await _configRepo.UpdateAsync(existing, ct);
        return existing;
    }

    public async Task<List<ReadReplicaConfig>> GetActiveReplicasAsync(CancellationToken ct)
    {
        var all = await _replicaRepo.FindAsync(r => r.IsEnabled, ct);
        return all.OrderBy(r => r.Priority).ToList();
    }

    public async Task<List<ReplicationScaleEvent>> GetScaleHistoryAsync(int limit, CancellationToken ct)
    {
        var all = await _scaleEventRepo.GetAllAsync(ct);
        return all.OrderByDescending(e => e.TriggeredAt).Take(limit).ToList();
    }

    public async Task<List<ReplicationLagAlert>> GetActiveAlertsAsync(CancellationToken ct)
    {
        var all = await _alertRepo.FindAsync(a => !a.IsResolved, ct);
        return all.OrderByDescending(a => a.TriggeredAt).ToList();
    }
}
