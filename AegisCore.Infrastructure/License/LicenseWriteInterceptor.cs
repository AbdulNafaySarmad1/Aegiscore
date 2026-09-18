using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Installation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AegisCore.Infrastructure.Licensing;

// Applies to EF writes from HTTP handlers and scheduled services alike.
public sealed class LicenseWriteInterceptor(IConfiguration configuration, IHostEnvironment environment, IHttpContextAccessor http) : SaveChangesInterceptor
{
    private static readonly HashSet<string> Maintenance = ["LicenseState", "AuditLog", "FerpaAuditLog", "SecurityAuditLog"];
    private bool RequiresCheck(DbContext? context)
    {
        if (context is not AegisDbContext || configuration.GetValue<bool>("Runtime:DemoMode")
            || !InstallationStateService.HasPersistentMarker(configuration, environment.ContentRootPath)) return false;
        var requestPath = http.HttpContext?.Request.Path;
        // Authentication must continue to work so users can read/export their records.
        if (requestPath?.StartsWithSegments("/api/auth") == true) return false;
        return context.ChangeTracker.Entries().Any(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            && !Maintenance.Contains(entry.Entity.GetType().Name));
    }

    private Guid Tenant(AegisDbContext db) => Guid.TryParse(configuration["TENANT_ID"], out var id) ? id : db.CurrentTenantId;
    private static void Check(LicenseState? state)
    {
        var validOverride = state?.OverrideUntil > DateTimeOffset.UtcNow;
        if (validOverride) return;
        if (state is null) throw new LicenseReadOnlyException("no licence has been activated");
        if (state.Status is "expired" or "invalid" or "revoked") throw new LicenseReadOnlyException("the licence is " + state.Status);
        if (state.LockedAt is not null || state.FailedHeartbeatCount >= 2) throw new LicenseReadOnlyException(state.LockReason == "heartbeat_failed" || state.FailedHeartbeatCount >= 2 ? "the licence authority could not be reached for two consecutive heartbeats" : "the licence is locked (" + (state.LockReason ?? "unknown reason") + ")");
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (RequiresCheck(eventData.Context)) {
            var db = (AegisDbContext)eventData.Context!;
            var tenant = Tenant(db);
            Check(db.LicenseStates.IgnoreQueryFilters().AsNoTracking().Where(s => s.TenantId == tenant).AsEnumerable().OrderByDescending(s => s.CreatedAt).FirstOrDefault());
        }
        return result;
    }
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (RequiresCheck(eventData.Context)) {
            var db = (AegisDbContext)eventData.Context!;
            var tenant = Tenant(db);
            var states = await db.LicenseStates.IgnoreQueryFilters().AsNoTracking().Where(s => s.TenantId == tenant).ToListAsync(cancellationToken);
            Check(states.OrderByDescending(s => s.CreatedAt).FirstOrDefault());
        }
        return result;
    }
}
