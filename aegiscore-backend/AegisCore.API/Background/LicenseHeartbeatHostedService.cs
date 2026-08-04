using AegisCore.Application.Licensing;

namespace AegisCore.API.Background;

public sealed class LicenseHeartbeatHostedService(IServiceScopeFactory scopes, ILogger<LicenseHeartbeatHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan HeartbeatTime = new(8, 0, 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.Now;
            var next = new DateTimeOffset(now.Year, now.Month, now.Day, HeartbeatTime.Hours, HeartbeatTime.Minutes, 0, now.Offset);
            if (now > next)
            {
                next = next.AddDays(1);
            }

            await Task.Delay(next - now, stoppingToken);

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
        }
    }
}
