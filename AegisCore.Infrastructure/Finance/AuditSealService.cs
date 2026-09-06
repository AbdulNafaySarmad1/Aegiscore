using System.Security.Cryptography;
using System.Text;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Http;
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
        var previous = await db.FinanceAuditSeals
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.SealedAt)
            .FirstOrDefaultAsync(ct);
        var start = previous?.PeriodEnd ?? DateTime.UnixEpoch;
        var events = await db.AuditLogs
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
            // Intentional cross-tenant discovery: this background worker enumerates tenant IDs only, then binds every operation to one tenant.
            var tenantIds = await db.AuditLogs.IgnoreQueryFilters().Select(x => x.TenantId).Distinct().ToListAsync(ct);
            var service = scope.ServiceProvider.GetRequiredService<FinanceAuditSealService>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
            foreach (var tenantId in tenantIds.Where(x => x != Guid.Empty))
            {
                tenantContext.HttpContext = new DefaultHttpContext();
                tenantContext.HttpContext.Items["TenantId"] = tenantId;
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

public sealed record FinanceLedgerIntegrityFailure(Guid EntryId, string EntryNumber, string Reason);
public sealed record FinanceLedgerIntegrityResult(bool IsValid, IReadOnlyList<FinanceLedgerIntegrityFailure> Failures);

public sealed class FinanceLedgerIntegrityService(AegisDbContext db, IPqcSignatureService pqc)
{
    public async Task<bool> VerifyAsync(Guid tenantId, CancellationToken ct = default)
        => (await VerifyDetailedAsync(tenantId, ct)).IsValid;

    public async Task<FinanceLedgerIntegrityResult> VerifyDetailedAsync(Guid tenantId, CancellationToken ct = default)
    {
        var entries = await db.JournalEntries.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsPosted)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.EntryNumber, x.PreviousHash, x.CanonicalPayload, x.EntryHash }).ToListAsync(ct);
        var signatures = await db.FinanceLedgerSignatures.AsNoTracking()
            .Where(x => x.TenantId == tenantId).ToDictionaryAsync(x => x.JournalEntryId, ct);
        byte[] previous = new byte[32];
        var failures = new List<FinanceLedgerIntegrityFailure>();
        foreach (var entry in entries)
        {
            if (entry.PreviousHash is null)
                failures.Add(new(entry.Id, entry.EntryNumber, "previous_hash_mismatch"));
            else if (!CryptographicOperations.FixedTimeEquals(previous, entry.PreviousHash))
                failures.Add(new(entry.Id, entry.EntryNumber, "previous_hash_mismatch"));
            if (entry.EntryHash is null || entry.CanonicalPayload is null)
            {
                failures.Add(new(entry.Id, entry.EntryNumber, "entry_hash_missing"));
                if (entry.EntryHash is not null) previous = entry.EntryHash;
                continue;
            }
            if (entry.PreviousHash is not null)
            {
                var calculated = SHA256.HashData([.. entry.PreviousHash, .. Encoding.UTF8.GetBytes(entry.CanonicalPayload)]);
                if (!CryptographicOperations.FixedTimeEquals(calculated, entry.EntryHash))
                    failures.Add(new(entry.Id, entry.EntryNumber, "entry_hash_mismatch"));
            }
            if (!signatures.TryGetValue(entry.Id, out var signature))
                failures.Add(new(entry.Id, entry.EntryNumber, "signature_missing"));
            else if (!pqc.Verify(signature.Algorithm, entry.EntryHash, signature.Signature, signature.PublicKey))
                failures.Add(new(entry.Id, entry.EntryNumber, "signature_invalid"));
            previous = entry.EntryHash;
        }
        return new(failures.Count == 0, failures);
    }
}

public sealed class FinanceLedgerSignatureWorker(IServiceScopeFactory scopes, ILogger<FinanceLedgerSignatureWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
                var signer = scope.ServiceProvider.GetRequiredService<IPqcSignatureService>();
                var tenantContext = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                // Intentional cross-tenant discovery: the worker reads unsignatured entry IDs and TenantIds, then binds before signing.
                var pending = await db.JournalEntries.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.IsPosted && x.EntryHash != null && !db.FinanceLedgerSignatures.IgnoreQueryFilters().Any(s => s.JournalEntryId == x.Id))
                    .OrderBy(x => x.CreatedAt).Take(100).Select(x => new { x.Id, x.TenantId, x.EntryHash }).ToListAsync(stoppingToken);
                foreach (var entry in pending)
                {
                    tenantContext.HttpContext = new DefaultHttpContext();
                    tenantContext.HttpContext.Items["TenantId"] = entry.TenantId;
                    var signed = await signer.SignAsync(entry.TenantId, "ledger-archive", entry.EntryHash!, archival: true, stoppingToken);
                    db.FinanceLedgerSignatures.Add(new FinanceLedgerSignature
                    {
                        Id = Guid.NewGuid(), TenantId = entry.TenantId, JournalEntryId = entry.Id, Algorithm = signed.Algorithm,
                        KeyId = signed.KeyId, Signature = signed.Signature, PublicKey = signed.PublicKey, SignedAt = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Ledger archival signing failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
