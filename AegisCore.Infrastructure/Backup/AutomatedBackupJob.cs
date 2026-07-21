using AegisCore.Application.Backup;
using Microsoft.Extensions.Logging;
using Quartz;

namespace AegisCore.Infrastructure.Backup;

[DisallowConcurrentExecution]
public class AutomatedBackupJob : IJob
{
    private readonly IBackupService _backupService;
    private readonly ILogger<AutomatedBackupJob> _logger;

    public AutomatedBackupJob(IBackupService backupService, ILogger<AutomatedBackupJob> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Automated backup job triggered at {Time}", DateTime.UtcNow);

        try
        {
            await _backupService.RunAllScheduledBackupsAsync(context.CancellationToken);
            await _backupService.CleanupExpiredBackupsAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Automated backup job failed");
        }
    }
}
