using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Http;
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
            // Intentional cross-tenant discovery: only tenant IDs are enumerated; each sweep is bound to one tenant below.
            var tenants = await db.FinancePaymentPlans.IgnoreQueryFilters().Select(x => x.TenantId).Distinct().ToListAsync(ct);
            var finance = scope.ServiceProvider.GetRequiredService<AegisCore.Application.Finance.IFinanceModuleService>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            foreach (var tenantId in tenants.Where(x => x != Guid.Empty))
            {
                tenantContext.HttpContext = new DefaultHttpContext();
                tenantContext.HttpContext.Items["TenantId"] = tenantId;
                await finance.RunPaymentPlanSweepAsync(tenantId, DateTime.UtcNow, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogError(ex, "Payment plan sweep failed."); }
    }
}
