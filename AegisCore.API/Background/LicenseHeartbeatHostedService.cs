using AegisCore.Application.Licensing;

namespace AegisCore.API.Background;

public sealed class LicenseHeartbeatHostedService(IServiceScopeFactory scopes, ILogger<LicenseHeartbeatHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var licenses = scope.ServiceProvider.GetRequiredService<ILicenseService>();
                await licenses.SendHeartbeatAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "License heartbeat job failed");
            }
            try { await Task.Delay(TimeSpan.FromHours(24), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
}
