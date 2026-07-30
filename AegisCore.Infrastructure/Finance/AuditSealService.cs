using System.Security.Cryptography;
using System.Text;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Finance;

public static class FinanceMerkleTree
{
    public static byte[] ComputeRoot(IReadOnlyList<byte[]> leaves)
    {
        if (leaves.Count == 0) return SHA256.HashData([]);
        var level = leaves.Select(Clone).ToList();
        while (level.Count > 1)
        {
            var next = new List<byte[]>((level.Count + 1) / 2);
            for (var index = 0; index < level.Count; index += 2)
            {
                var right = index + 1 < level.Count ? level[index + 1] : level[index];
                next.Add(SHA256.HashData([.. level[index], .. right]));
            }
            level = next;
        }
        return level[0];
    }

    private static byte[] Clone(byte[] value) => [.. value];
}

public sealed class FinanceAuditSealService(AegisDbContext db, IPqcSignatureService pqc)
{
    public async Task<FinanceAuditSeal?> SealTenantAsync(Guid tenantId, DateTime now, CancellationToken ct)
    {
        var previous = await db.FinanceAuditSeals.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.SealedAt)
            .FirstOrDefaultAsync(ct);
        var start = previous?.PeriodEnd ?? DateTime.UnixEpoch;
        var events = await db.AuditLogs.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Timestamp > start && x.Timestamp <= now)
            .OrderBy(x => x.Timestamp).ThenBy(x => x.Id)
            .ToListAsync(ct);
        if (events.Count == 0) return null;

        var leaves = events.Select(EventHash).ToList();
        var root = FinanceMerkleTree.ComputeRoot(leaves);
        var payload = Encoding.UTF8.GetBytes($"{tenantId:D}|{start:O}|{now:O}|{events.Count}|{Convert.ToHexString(root)}");
        var signed = await pqc.SignAsync(tenantId, "audit-seal", payload, archival: false, ct);
        var seal = new FinanceAuditSeal
        {
            Id = Guid.NewGuid(), TenantId = tenantId, PeriodStart = start, PeriodEnd = now,
            EventCount = events.Count, Algorithm = signed.Algorithm, KeyId = signed.KeyId,
            MerkleRoot = root, Signature = signed.Signature, PublicKey = signed.PublicKey, SealedAt = now
        };
        db.FinanceAuditSeals.Add(seal);
        await db.SaveChangesAsync(ct);
        return seal;
    }

    internal static byte[] EventHash(AuditLog audit)
    {
        var canonical = $"{audit.TenantId:D}|{audit.Id:D}|{audit.Timestamp:O}|{audit.UserId:D}|{audit.Action}|{audit.ResourceType}|{audit.ResourceId:D}|{audit.Success}|{audit.IpAddress ?? string.Empty}|{audit.Details ?? string.Empty}";
        return SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
    }
}

public sealed class FinanceAuditSealWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<FinanceAuditSealWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SealAllTenantsAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await SealAllTenantsAsync(stoppingToken);
    }

    private async Task SealAllTenantsAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
            var tenantIds = await db.AuditLogs.IgnoreQueryFilters().Select(x => x.TenantId).Distinct().ToListAsync(ct);
            var service = scope.ServiceProvider.GetRequiredService<FinanceAuditSealService>();
            foreach (var tenantId in tenantIds.Where(x => x != Guid.Empty))
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.tenant_id', {tenantId:D}, false)", ct);
                await service.SealTenantAsync(tenantId, DateTime.UtcNow, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Finance audit sealing cycle failed.");
        }
    }
}
