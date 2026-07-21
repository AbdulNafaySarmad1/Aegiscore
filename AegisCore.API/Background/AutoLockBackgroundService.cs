using AegisCore.API.Hubs;
using AegisCore.Infrastructure.Workflow.StateMachine;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisCore.API.Background;

public class AutoLockBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoLockBackgroundService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

    public AutoLockBackgroundService(IServiceProvider serviceProvider, ILogger<AutoLockBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoLockBackgroundService started.");
        using var timer = new PeriodicTimer(_period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var stateMachine = scope.ServiceProvider.GetRequiredService<IStateMachineService>();
                var hubContext = scope.ServiceProvider.GetService<IHubContext<StateMachineHub>>();
                await stateMachine.SetStateAsync("autolock:heartbeat", DateTime.UtcNow.ToString("O"), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoLockBackgroundService tick failed.");
            }
        }
    }
}
