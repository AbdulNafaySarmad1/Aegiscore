using System.Diagnostics;
using System.Security.Cryptography;
using AegisCore.Application.Backup;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AegisCore.Infrastructure.Backup;

public class BackupService : IBackupService
{
    private readonly IBackupConfigurationRepository _configRepo;
    private readonly IBackupRecordRepository _recordRepo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IBackupConfigurationRepository configRepo,
        IBackupRecordRepository recordRepo,
        IConfiguration configuration,
        ILogger<BackupService> logger)
    {
        _configRepo = configRepo;
        _recordRepo = recordRepo;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<BackupRecord> RunBackupAsync(Guid configurationId, string triggeredBy, CancellationToken cancellationToken = default)
    {
        var config = await _configRepo.GetByIdAsync(configurationId, cancellationToken)
            ?? throw new InvalidOperationException("Backup configuration not found.");

        var record = new BackupRecord
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            BackupType = config.BackupType,
            Status = BackupStatus.Running,
            IsEncrypted = config.EncryptBackup,
            RetentionTier = BackupRetentionTier.Hot,
            StartedAt = DateTime.UtcNow,
            TriggeredBy = triggeredBy,
            TenantId = config.TenantId
        };

        await _recordRepo.AddAsync(record, cancellationToken);

        try
        {
            var dbName = config.DatabaseName ?? _configuration.GetConnectionString("PostgresDatabase") ?? DefaultTenantConfigTemplate.DefaultDatabaseName;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{dbName}_{config.BackupType}_{timestamp}.sql.gz";
            var filePath = Path.Combine(config.StoragePath, fileName);

            Directory.CreateDirectory(config.StoragePath);

            var connectionString = _configuration.GetConnectionString("SupabasePostgres")
                ?? _configuration["Postgres:ConnectionString"]
                ?? DefaultTenantConfigTemplate.DefaultPostgresConnectionString;

            await ExecutePgDumpAsync(connectionString, dbName, filePath, config.BackupType, cancellationToken);

            var fileInfo = new FileInfo(filePath);
            record.FilePath = filePath;
            record.SizeBytes = fileInfo.Exists ? fileInfo.Length : 0;
            record.Checksum = fileInfo.Exists ? ComputeChecksum(filePath) : null;
            record.Status = BackupStatus.Completed;
            record.CompletedAt = DateTime.UtcNow;
            record.ExpiresAt = DateTime.UtcNow.AddDays(config.RetentionDaysHot);

            _logger.LogInformation("Backup completed: {File} ({Size} bytes, {Checksum})",
                fileName, record.SizeBytes, record.Checksum);

            if (config.VerifyAfterBackup)
            {
                await VerifyBackupAsync(record.Id, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            record.Status = BackupStatus.Failed;
            record.CompletedAt = DateTime.UtcNow;
            record.ErrorMessage = "Backup operation failed. Check system logs.";
            _logger.LogError(ex, "Backup failed for configuration {ConfigId}", configurationId);
        }

        config.LastRunAt = DateTime.UtcNow;
        config.NextRunAt = CalculateNextRun(config.CronExpression);
        await _configRepo.UpdateAsync(config, cancellationToken);
        await _recordRepo.UpdateAsync(record, cancellationToken);

        return record;
    }

    public async Task<BackupRecord> RunAllScheduledBackupsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var configs = await _configRepo.GetScheduledBeforeAsync(now, cancellationToken);
        BackupRecord? lastRecord = null;

        foreach (var config in configs)
        {
            lastRecord = await RunBackupAsync(config.Id, "Scheduled", cancellationToken);
        }

        return lastRecord ?? new BackupRecord { Status = BackupStatus.Completed };
    }

    public async Task VerifyBackupAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var record = await _recordRepo.GetByIdAsync(recordId, cancellationToken)
            ?? throw new InvalidOperationException("Backup record not found.");

        if (!File.Exists(record.FilePath))
        {
            record.Status = BackupStatus.Failed;
            record.ErrorMessage = "Backup file not found for verification.";
            await _recordRepo.UpdateAsync(record, cancellationToken);
            return;
        }

        try
        {
            var currentChecksum = ComputeChecksum(record.FilePath);
            if (currentChecksum == record.Checksum)
            {
                record.IsVerified = true;
                record.VerifiedAt = DateTime.UtcNow;
                record.Status = BackupStatus.Verified;
                _logger.LogInformation("Backup verified: {File}", record.FilePath);
            }
            else
            {
                record.ErrorMessage = $"Checksum mismatch. Expected: {record.Checksum}, Actual: {currentChecksum}";
                _logger.LogWarning("Backup verification failed: {File}", record.FilePath);
            }
        }
        catch (Exception ex)
        {
            record.ErrorMessage = "Verification failed. Check system logs.";
            _logger.LogWarning(ex, "Backup verification failed for {RecordId}", recordId);
        }

        await _recordRepo.UpdateAsync(record, cancellationToken);
    }

    public async Task CleanupExpiredBackupsAsync(CancellationToken cancellationToken = default)
    {
        var expired = await _recordRepo.GetExpiredAsync(DateTime.UtcNow, cancellationToken);

        foreach (var record in expired)
        {
            try
            {
                if (File.Exists(record.FilePath))
                    File.Delete(record.FilePath);

                record.RetentionTier = BackupRetentionTier.Cold;
                await _recordRepo.UpdateAsync(record, cancellationToken);
                _logger.LogInformation("Cleaned up expired backup: {File}", record.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up backup: {File}", record.FilePath);
            }
        }
    }

    public async Task<BackupRecord> RestoreFromBackupAsync(Guid recordId, DateTime? pointInTime = null, CancellationToken cancellationToken = default)
    {
        var record = await _recordRepo.GetByIdAsync(recordId, cancellationToken)
            ?? throw new InvalidOperationException("Backup record not found.");

        if (!File.Exists(record.FilePath))
            throw new InvalidOperationException($"Backup file not found: {record.FilePath}");

        if (!record.IsVerified && record.Status == BackupStatus.Completed)
        {
            _logger.LogWarning("Restoring from unverified backup: {RecordId}. Running verification first.", recordId);
            await VerifyBackupAsync(recordId, cancellationToken);
        }

        var connectionString = _configuration.GetConnectionString("SupabasePostgres")
            ?? _configuration["Postgres:ConnectionString"]
            ?? DefaultTenantConfigTemplate.DefaultPostgresConnectionString;

        if (pointInTime.HasValue)
        {
            await ExecutePitrRestoreAsync(connectionString, record.FilePath, pointInTime.Value, cancellationToken);
        }
        else
        {
            await ExecutePgRestoreAsync(connectionString, record.FilePath, cancellationToken);
        }

        _logger.LogInformation("Restore completed from backup {RecordId} (PITR: {Pitr})", recordId, pointInTime);

        return record;
    }

    private async Task ExecutePgDumpAsync(string connectionString, string dbName, string filePath, BackupType type, CancellationToken ct)
    {
        var tool = type == BackupType.WalArchive ? Tool("pg_receivewal") : Tool("pg_dump");
        var args = type == BackupType.WalArchive
            ? BuildCommonArgs(connectionString, dbName).Concat(["-D", filePath]).ToArray()
            : BuildCommonArgs(connectionString, dbName).Concat(["-Fc", "-f", filePath]).ToArray();

        _logger.LogInformation("Running PostgreSQL backup tool {Tool} for {File}", tool, filePath);
        await ExecutePostgresToolAsync(tool, args, connectionString, ct);
    }

    private async Task ExecutePgRestoreAsync(string connectionString, string filePath, CancellationToken ct)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database ?? throw new InvalidOperationException("Database name is required for restore.");
        var tool = Tool("pg_restore");
        var args = BuildCommonArgs(connectionString, dbName).Concat(["-c", "-v", filePath]).ToArray();

        _logger.LogInformation("Running PostgreSQL restore for {File}", filePath);
        await ExecutePostgresToolAsync(tool, args, connectionString, ct);
    }

    private Task ExecutePitrRestoreAsync(string connectionString, string filePath, DateTime pointInTime, CancellationToken ct)
    {
        throw new NotSupportedException("Point-in-time restore requires WAL archive orchestration outside pg_restore. Use the disaster recovery runbook for PITR.");
    }

    private string Tool(string name) => _configuration[$"PostgresTools:{name}"] ?? name;

    private static string[] BuildCommonArgs(string connectionString, string dbName)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var args = new List<string>();
        if (!string.IsNullOrWhiteSpace(builder.Host)) args.AddRange(["-h", builder.Host]);
        if (builder.Port > 0) args.AddRange(["-p", builder.Port.ToString()]);
        if (!string.IsNullOrWhiteSpace(builder.Username)) args.AddRange(["-U", builder.Username]);
        args.AddRange(["-d", dbName]);
        return args.ToArray();
    }

    private static async Task ExecutePostgresToolAsync(string tool, IEnumerable<string> args, string connectionString, CancellationToken ct)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var start = new ProcessStartInfo(tool)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        foreach (var arg in args)
            start.ArgumentList.Add(arg);

        if (!string.IsNullOrEmpty(builder.Password))
            start.Environment["PGPASSWORD"] = builder.Password;

        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Failed to start {tool}.");
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        var stderr = await stderrTask;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{tool} failed with exit code {process.ExitCode}: {stderr}");
    }

    private static string ComputeChecksum(string filePath)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    private static DateTime CalculateNextRun(string cronExpression)
    {
        try
        {
            var expr = Cronos.CronExpression.Parse(cronExpression, Cronos.CronFormat.IncludeSeconds);
            var next = expr.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
            return next ?? DateTime.UtcNow.AddHours(24);
        }
        catch
        {
            return DateTime.UtcNow.AddHours(24);
        }
    }
}
