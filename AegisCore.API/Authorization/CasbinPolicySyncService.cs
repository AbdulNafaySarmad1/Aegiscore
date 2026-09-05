using Casbin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AegisCore.API.Authorization;

public interface ICasbinPolicySyncService
{
    Task SyncTenantAsync(Guid tenantId, CancellationToken ct = default, bool force = false);
    Task<bool> EnforceAsync(Guid tenantId, IEnumerable<string> roles, string resource, string action, CancellationToken ct = default);
    void Invalidate(Guid tenantId);
}

public sealed class CasbinPolicySyncService(
    AegisCore.Infrastructure.AegisDbContext db,
    IEnforcer enforcer,
    IMemoryCache cache) : ICasbinPolicySyncService
{
    private static readonly SemaphoreSlim PolicyGate = new(1, 1);
    private static readonly TimeSpan PolicyTtl = TimeSpan.FromMinutes(5);

    public async Task SyncTenantAsync(Guid tenantId, CancellationToken ct = default, bool force = false)
    {
        var cacheKey = CacheKey(tenantId);
        if (!force && cache.TryGetValue(cacheKey, out _)) return;

        await PolicyGate.WaitAsync(ct);
        try
        {
            if (!force && cache.TryGetValue(cacheKey, out _)) return;
            var grants = await db.RolePermissions.IgnoreQueryFilters().AsNoTracking()
                .Where(grant => grant.TenantId == tenantId)
                .Select(grant => new { grant.Role.Name, grant.Permission.Resource, grant.Permission.Action })
                .ToListAsync(ct);

            var prefix = SubjectPrefix(tenantId);
            var oldPolicies = enforcer.GetPolicy().Where(policy =>
                policy.FirstOrDefault()?.StartsWith(prefix, StringComparison.Ordinal) == true).ToArray();
            foreach (var policy in oldPolicies)
                await enforcer.RemovePolicyAsync(policy);
            foreach (var grant in grants.DistinctBy(item => $"{item.Name}:{item.Resource}:{item.Action}", StringComparer.OrdinalIgnoreCase))
                await enforcer.AddPolicyAsync(Subject(tenantId, grant.Name), Normalize(grant.Resource), Normalize(grant.Action));

            cache.Set(cacheKey, true, PolicyTtl);
        }
        finally
        {
            PolicyGate.Release();
        }
    }

    public async Task<bool> EnforceAsync(Guid tenantId, IEnumerable<string> roles, string resource, string action, CancellationToken ct = default)
    {
        await SyncTenantAsync(tenantId, ct);
        await PolicyGate.WaitAsync(ct);
        try
        {
            foreach (var role in roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase))
                if (await enforcer.EnforceAsync(Subject(tenantId, role), Normalize(resource), Normalize(action))) return true;
            return false;
        }
        finally
        {
            PolicyGate.Release();
        }
    }

    public void Invalidate(Guid tenantId) => cache.Remove(CacheKey(tenantId));

    private static string SubjectPrefix(Guid tenantId) => $"tenant:{tenantId:N}:role:";
    private static string Subject(Guid tenantId, string role) => SubjectPrefix(tenantId) + Normalize(role);
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string CacheKey(Guid tenantId) => $"casbin:tenant-policy:{tenantId:N}";
}
