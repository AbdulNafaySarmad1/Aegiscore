namespace AegisCore.Application.Backup;

public class BackupConfigurationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BackupType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int RetentionDaysHot { get; set; }
    public int RetentionDaysCold { get; set; }
    public bool EncryptBackup { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
}

public class BackupRecordDto
{
    public Guid Id { get; set; }
    public string BackupType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Checksum { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsVerified { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CreateBackupConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string BackupType { get; set; } = "Full";
    public string CronExpression { get; set; } = "0 0 2 * * ?";
    public int RetentionDaysHot { get; set; } = 30;
    public bool EncryptBackup { get; set; } = true;
    public string StoragePath { get; set; } = "/backups";
}

public class RestoreRequest
{
    public Guid BackupRecordId { get; set; }
    public DateTime? PointInTime { get; set; }
}

public class ReplicaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? CurrentLagSeconds { get; set; }
    public int Weight { get; set; }
    public bool IsEnabled { get; set; }
}

public class HealthCheckReport
{
    public bool PrimaryHealthy { get; set; }
    public bool RedisHealthy { get; set; }
    public int OnlineReplicas { get; set; }
    public int TotalReplicas { get; set; }
    public int MaxReplicaLagSeconds { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<ReplicaDto> Replicas { get; set; } = new();
}
