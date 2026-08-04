using AegisCore.Application.Backup;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Backup;

public class DatabaseHealthService : IDatabaseHealthService
{
    private readonly IReadReplicaConfigRepository _replicaRepo;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseHealthService> _logger;

    public DatabaseHealthService(
        IReadReplicaConfigRepository replicaRepo,
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<DatabaseHealthService> logger)
    {
        _replicaRepo = replicaRepo;
        _redis = redis;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> IsPrimaryHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("SupabasePostgres")
                ?? _configuration["Postgres:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString)) return false;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null && (int)result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary database health check failed");
            return false;
        }
    }

    public async Task<bool> IsReplicaLaggingAsync(Guid replicaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var replica = await _replicaRepo.GetByIdAsync(replicaId, cancellationToken);
            if (replica == null) return true;

            await using var connection = new NpgsqlConnection(replica.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(
                "SELECT COALESCE(EXTRACT(EPOCH FROM (NOW() - pg_last_xact_replay_timestamp())), 0)::int",
                connection);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var lag = result is long l ? (int)l : (result is int i ? i : 0);

            replica.CurrentLagSeconds = lag;

            if (lag > replica.MaxLagSeconds)
            {
                replica.Status = ReplicaStatus.Lagging;
                _logger.LogWarning("Replica {ReplicaName} is lagging: {Lag}s (max: {Max}s)",
                    replica.Name, lag, replica.MaxLagSeconds);
            }
            else
            {
                replica.Status = ReplicaStatus.Online;
            }

            replica.LastHealthCheckAt = DateTime.UtcNow;
            await _replicaRepo.UpdateAsync(replica, cancellationToken);

            return lag > replica.MaxLagSeconds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Replica health check failed for {ReplicaId}", replicaId);
            return true;
        }
    }

    public async Task<IReadOnlyList<Domain.Entities.ReadReplicaConfig>> GetHealthyReplicasAsync(CancellationToken cancellationToken = default)
    {
        var replicas = await _replicaRepo.GetOnlineAsync(cancellationToken);
        var healthy = new List<Domain.Entities.ReadReplicaConfig>();

        foreach (var replica in replicas)
        {
            var isLagging = await IsReplicaLaggingAsync(replica.Id, cancellationToken);
            if (!isLagging)
                healthy.Add(replica);
        }

        return healthy;
    }

    public async Task PromoteReplicaAsync(Guid replicaId, CancellationToken cancellationToken = default)
    {
        var replica = await _replicaRepo.GetByIdAsync(replicaId, cancellationToken)
            ?? throw new InvalidOperationException("Replica not found.");

        replica.Status = ReplicaStatus.Promoting;
        await _replicaRepo.UpdateAsync(replica, cancellationToken);

        _logger.LogWarning("Replica {ReplicaName} promotion initiated. Manual intervention may be required.", replica.Name);

        // In production: execute pg_ctl promote on the replica, update connection strings, swap primary
    }
}
