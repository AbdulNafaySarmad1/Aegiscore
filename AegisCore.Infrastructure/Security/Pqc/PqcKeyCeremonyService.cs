using System.Data;
using System.Security.Cryptography;
using System.Text;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Security.Pqc;

public sealed record CreatePqcKeyCeremonyCommand(string Purpose, string Algorithm, int RequiredApprovals = 2, int ValidForHours = 72);

public sealed class PqcKeyCeremonyService(AegisDbContext db, IPqcSignatureService signatures)
{
    public async Task<FinancePqcKeyCeremony> CreateAsync(Guid tenantId, Guid requesterId, CreatePqcKeyCeremonyCommand command, CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty || requesterId == Guid.Empty) throw new ArgumentException("Tenant and requester are required.");
        var purpose = NormalizePurpose(command.Purpose);
        var algorithm = NormalizeAlgorithm(command.Algorithm);
        if (command.RequiredApprovals is < 2 or > 20) throw new ArgumentOutOfRangeException(nameof(command.RequiredApprovals));
        if (command.ValidForHours is < 1 or > 168) throw new ArgumentOutOfRangeException(nameof(command.ValidForHours));
        var pending = await db.FinancePqcKeyCeremonies.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Purpose == purpose && x.Status == "PENDING" && x.ExpiresAt > DateTime.UtcNow, ct);
        if (pending is not null) return pending;
        var ceremony = new FinancePqcKeyCeremony
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Purpose = purpose, Algorithm = algorithm,
            RequiredApprovals = command.RequiredApprovals, RequestedByUserId = requesterId,
            CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddHours(command.ValidForHours)
        };
        db.FinancePqcKeyCeremonies.Add(ceremony);
        await db.SaveChangesAsync(ct);
        return ceremony;
    }

    public async Task<FinancePqcKeyCeremony> ApproveAsync(Guid tenantId, Guid ceremonyId, Guid approverId, CancellationToken ct = default)
    {
        if (approverId == Guid.Empty) throw new ArgumentException("Approver is required.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var ceremony = await db.FinancePqcKeyCeremonies.Include(x => x.Approvals)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == ceremonyId, ct)
            ?? throw new KeyNotFoundException("PQC key ceremony was not found.");
        if (ceremony.Status != "PENDING" || ceremony.ExpiresAt <= DateTime.UtcNow) throw new InvalidOperationException("PQC key ceremony is not active.");
        if (ceremony.RequestedByUserId == approverId) throw new InvalidOperationException("The requester cannot approve the same key ceremony.");
        if (ceremony.Approvals.Any(x => x.ApprovedByUserId == approverId)) throw new InvalidOperationException("This user has already approved the key ceremony.");
        var approvedAt = DateTime.UtcNow;
        ceremony.Approvals.Add(new FinancePqcKeyCeremonyApproval
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CeremonyId = ceremony.Id, ApprovedByUserId = approverId,
            ApprovedAt = approvedAt, ApprovalDigest = Digest(ceremony, approverId, approvedAt)
        });
        if (ceremony.Approvals.Count >= ceremony.RequiredApprovals)
        {
            ceremony.ActivatedKeyId = await signatures.RotateAsync(tenantId, ceremony.Purpose, ceremony.Algorithm == "SLH-DSA-SHA2-128s", ct);
            ceremony.Status = "COMPLETED";
            ceremony.CompletedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return ceremony;
    }

    private static byte[] Digest(FinancePqcKeyCeremony ceremony, Guid approverId, DateTime approvedAt)
        => SHA256.HashData(Encoding.UTF8.GetBytes($"{ceremony.TenantId:D}|{ceremony.Id:D}|{ceremony.Purpose}|{ceremony.Algorithm}|{approverId:D}|{approvedAt:O}"));

    private static string NormalizePurpose(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length is < 1 or > 50 || normalized.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-'))) throw new ArgumentException("PQC key purpose is invalid.");
        return normalized;
    }

    private static string NormalizeAlgorithm(string value) => value.Trim().ToUpperInvariant() switch
    {
        "ML-DSA-65" => "ML-DSA-65",
        "SLH-DSA-SHA2-128S" => "SLH-DSA-SHA2-128s",
        _ => throw new ArgumentException("Unsupported PQC key algorithm.")
    };
}

public sealed class PqcKeyRotationWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<PqcKeyRotationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(12));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
                var ceremonies = scope.ServiceProvider.GetRequiredService<PqcKeyCeremonyService>();
                var archivalLeadDays = configuration.GetValue("Pqc:Rotation:ArchivalLeadDays", 90);
                var signingLeadDays = configuration.GetValue("Pqc:Rotation:SigningLeadDays", 14);
                var requiredApprovals = configuration.GetValue("Pqc:Rotation:RequiredApprovals", 2);
                var validForHours = configuration.GetValue("Pqc:Rotation:ValidForHours", 168);
                var custodianRole = configuration["Pqc:Rotation:CustodianRole"]
                    ?? throw new InvalidOperationException("Pqc:Rotation:CustodianRole is required.");
                // Intentional cross-tenant discovery: expiring key purpose/TenantId pairs are enumerated, then each rotation is tenant-bound.
                var due = await db.FinancePqcKeys.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.IsActive && x.ExpiresAt <= DateTime.UtcNow.AddDays(x.Algorithm == "slh-dsa-sha2-128s" ? archivalLeadDays : signingLeadDays))
                    .Select(x => new { x.TenantId, x.Purpose, x.Algorithm }).Distinct().ToListAsync(stoppingToken);
                foreach (var key in due)
                {
                    var tenantContext = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                    tenantContext.HttpContext = new DefaultHttpContext();
                    tenantContext.HttpContext.Items["TenantId"] = key.TenantId;
                    var custodianId = await db.UserRoles.AsNoTracking()
                        .Where(x => x.TenantId == key.TenantId && x.Role.Name == custodianRole && (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow))
                        .OrderBy(x => x.AssignedAt).Select(x => x.UserId).FirstOrDefaultAsync(stoppingToken);
                    if (custodianId == Guid.Empty)
                    {
                        logger.LogError("PQC key rotation cannot start for tenant {TenantId}: no active custodian is assigned.", key.TenantId);
                        continue;
                    }
                    await ceremonies.CreateAsync(key.TenantId, custodianId,
                        new CreatePqcKeyCeremonyCommand(key.Purpose, key.Algorithm, requiredApprovals, validForHours), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "PQC key rotation scheduling failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
