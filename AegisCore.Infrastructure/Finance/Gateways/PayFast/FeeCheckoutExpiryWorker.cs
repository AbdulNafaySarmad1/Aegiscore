using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Finance.Gateways.PayFast;

/// <summary>
/// Every five minutes, closes PayFast attempts that passed their expiry without PayFast confirming
/// anything, so an abandoned checkout never leaves a fee demand in PAYMENT_PENDING.
/// </summary>
public sealed class FeeCheckoutExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<FeeCheckoutExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do await SweepAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
            var now = DateTime.UtcNow;
            // Intentional cross-tenant discovery: only tenant IDs are enumerated; each sweep is bound to one tenant below.
            var tenants = await db.FeeCheckoutSessions.IgnoreQueryFilters()
                .Where(s => s.Status == "PENDING" && s.ExpiresAt <= now).Select(s => s.TenantId).Distinct().ToListAsync(ct);
            foreach (var tenantId in tenants.Where(x => x != Guid.Empty))
            {
                await using var tenantScope = scopeFactory.CreateAsyncScope();
                var accessor = tenantScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                accessor.HttpContext = new DefaultHttpContext();
                accessor.HttpContext.Items["TenantId"] = tenantId;
                var checkout = tenantScope.ServiceProvider.GetRequiredService<PayFastCheckoutService>();
                var closed = await checkout.ExpireStaleAsync(tenantId, now, ct);
                if (closed > 0) logger.LogInformation("Expired {Count} PayFast checkout attempts for tenant {TenantId}.", closed, tenantId);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogError(ex, "PayFast checkout expiry sweep failed."); }
    }
}
