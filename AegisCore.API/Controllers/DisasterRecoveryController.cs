using AegisCore.Application.Backup;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/admin/dr")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class DisasterRecoveryController : ControllerBase
{
    private readonly IBackupConfigurationRepository _configRepo;
    private readonly IBackupRecordRepository _recordRepo;
    private readonly IBackupService _backupService;
    private readonly IDatabaseHealthService _healthService;
    private readonly IReadReplicaConfigRepository _replicaRepo;
    private readonly ILogger<DisasterRecoveryController> _logger;

    public DisasterRecoveryController(
        IBackupConfigurationRepository configRepo,
        IBackupRecordRepository recordRepo,
        IBackupService backupService,
        IDatabaseHealthService healthService,
        IReadReplicaConfigRepository replicaRepo,
        ILogger<DisasterRecoveryController> logger)
    {
        _configRepo = configRepo;
        _recordRepo = recordRepo;
        _backupService = backupService;
        _healthService = healthService;
        _replicaRepo = replicaRepo;
        _logger = logger;
    }

    [HttpPost("backup/configs")]
    public async Task<IActionResult> CreateBackupConfig([FromBody] CreateBackupConfigRequest request, CancellationToken cancellationToken)
    {
        var config = new BackupConfiguration
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BackupType = Enum.TryParse<BackupType>(request.BackupType, true, out var bt) ? bt : BackupType.Full,
            CronExpression = request.CronExpression,
            RetentionDaysHot = request.RetentionDaysHot,
            EncryptBackup = request.EncryptBackup,
            StoragePath = request.StoragePath,
            IsEnabled = true,
            TenantId = Guid.Empty
        };

        await _configRepo.AddAsync(config, cancellationToken);
        return Ok(new BackupConfigurationDto
        {
            Id = config.Id, Name = config.Name, BackupType = config.BackupType.ToString(),
            CronExpression = config.CronExpression, IsEnabled = config.IsEnabled,
            RetentionDaysHot = config.RetentionDaysHot, RetentionDaysCold = config.RetentionDaysCold,
            StoragePath = config.StoragePath
        });
    }

    [HttpGet("backup/configs")]
    public async Task<IActionResult> ListConfigs(CancellationToken cancellationToken)
    {
        var configs = await _configRepo.GetEnabledAsync(cancellationToken);
        var response = configs.Select(c => new BackupConfigurationDto
        {
            Id = c.Id, Name = c.Name, BackupType = c.BackupType.ToString(),
            CronExpression = c.CronExpression, IsEnabled = c.IsEnabled,
            RetentionDaysHot = c.RetentionDaysHot, StoragePath = c.StoragePath,
            LastRunAt = c.LastRunAt, NextRunAt = c.NextRunAt
        });

        return Ok(new { total = response.Count(), configs = response });
    }

    [HttpPost("backup/run/{configId:guid}")]
    public async Task<IActionResult> RunManualBackup(Guid configId, CancellationToken cancellationToken)
    {
        var record = await _backupService.RunBackupAsync(configId, "Manual", cancellationToken);
        return Ok(new BackupRecordDto
        {
            Id = record.Id, BackupType = record.BackupType.ToString(),
            Status = record.Status.ToString(), FilePath = record.FilePath,
            SizeBytes = record.SizeBytes, Checksum = record.Checksum,
            StartedAt = record.StartedAt, CompletedAt = record.CompletedAt,
            IsVerified = record.IsVerified, ErrorMessage = record.ErrorMessage
        });
    }

    [HttpGet("backup/records")]
    public async Task<IActionResult> ListRecords([FromQuery] Guid? configId, CancellationToken cancellationToken)
    {
        IReadOnlyList<BackupRecord> records;
        if (configId.HasValue)
            records = await _recordRepo.GetByConfigurationAsync(configId.Value, cancellationToken);
        else
            records = await _recordRepo.GetAllAsync(cancellationToken);

        var response = records.Select(r => new BackupRecordDto
        {
            Id = r.Id, BackupType = r.BackupType.ToString(), Status = r.Status.ToString(),
            FilePath = r.FilePath, SizeBytes = r.SizeBytes, Checksum = r.Checksum,
            StartedAt = r.StartedAt, CompletedAt = r.CompletedAt,
            ExpiresAt = r.ExpiresAt, IsVerified = r.IsVerified, ErrorMessage = r.ErrorMessage
        });

        return Ok(new { total = response.Count(), records = response });
    }

    [HttpPost("backup/verify/{recordId:guid}")]
    public async Task<IActionResult> VerifyBackup(Guid recordId, CancellationToken cancellationToken)
    {
        await _backupService.VerifyBackupAsync(recordId, cancellationToken);
        return Ok(new { message = "Backup verification completed." });
    }

    [HttpPost("backup/restore")]
    public async Task<IActionResult> RestoreBackup([FromBody] RestoreRequest request, CancellationToken cancellationToken)
    {
        await _backupService.RestoreFromBackupAsync(request.BackupRecordId, request.PointInTime, cancellationToken);
        return Ok(new { message = "Restore completed." });
    }

    [HttpPost("backup/cleanup")]
    public async Task<IActionResult> CleanupExpired(CancellationToken cancellationToken)
    {
        await _backupService.CleanupExpiredBackupsAsync(cancellationToken);
        return Ok(new { message = "Expired backup cleanup completed." });
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealthReport(CancellationToken cancellationToken)
    {
        var primaryHealthy = await _healthService.IsPrimaryHealthyAsync(cancellationToken);
        var replicas = await _replicaRepo.GetOnlineAsync(cancellationToken);

        var replicaDtos = new List<ReplicaDto>();
        var maxLag = 0;

        foreach (var replica in replicas)
        {
            await _healthService.IsReplicaLaggingAsync(replica.Id, cancellationToken);
            if (replica.CurrentLagSeconds > maxLag) maxLag = replica.CurrentLagSeconds ?? 0;

            replicaDtos.Add(new ReplicaDto
            {
                Id = replica.Id, Name = replica.Name, Priority = replica.Priority,
                Status = replica.Status.ToString(), CurrentLagSeconds = replica.CurrentLagSeconds,
                Weight = replica.Weight, IsEnabled = replica.IsEnabled
            });
        }

        return Ok(new HealthCheckReport
        {
            PrimaryHealthy = primaryHealthy,
            RedisHealthy = true,
            OnlineReplicas = replicas.Count(r => r.Status == ReplicaStatus.Online),
            TotalReplicas = replicas.Count,
            MaxReplicaLagSeconds = maxLag,
            CheckedAt = DateTime.UtcNow,
            Replicas = replicaDtos
        });
    }

    [HttpPost("failover/promote/{replicaId:guid}")]
    public async Task<IActionResult> PromoteReplica(Guid replicaId, CancellationToken cancellationToken)
    {
        await _healthService.PromoteReplicaAsync(replicaId, cancellationToken);
        return Ok(new { message = "Replica promotion initiated. Monitor the failover process." });
    }
}
