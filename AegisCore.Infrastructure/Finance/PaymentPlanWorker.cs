using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Finance;

public sealed class FinancePaymentPlanWorker(IServiceScopeFactory scopeFactory, ILogger<FinancePaymentPlanWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SweepAllTenantsAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await SweepAllTenantsAsync(stoppingToken);
    }

    private async Task SweepAllTenantsAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
            var tenants = await db.FinancePaymentPlans.IgnoreQueryFilters().Select(x => x.TenantId).Distinct().ToListAsync(ct);
            var finance = scope.ServiceProvider.GetRequiredService<AegisCore.Application.Finance.IFinanceModuleService>();
            foreach (var tenantId in tenants.Where(x => x != Guid.Empty))
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.tenant_id', {tenantId:D}, false)", ct);
                await finance.RunPaymentPlanSweepAsync(tenantId, DateTime.UtcNow, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogError(ex, "Payment plan sweep failed."); }
    }
}
