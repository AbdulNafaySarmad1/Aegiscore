using System.Text;
using AegisCore.Application.Compliance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Pqc;

namespace AegisCore.Infrastructure.Compliance;

/// <summary>
/// Records reads of sensitive non-student records in the same log the FERPA-style student audit
/// uses (one table, one report at <c>GET /api/admin/ferpa-audit</c>), with the IP encrypted the
/// same way. <c>Justified</c> is true because the permission layer already allowed the request;
/// <c>StudentId</c> is empty because the subject is not a student record.
/// </summary>
public sealed class AccessAuditService(AegisDbContext db, IPqcCryptographyService encryption) : IAccessAuditService
{
    public async Task RecordAsync(AccessAuditRecord record, CancellationToken cancellationToken = default)
    {
        var tenantId = db.CurrentTenantId;
        if (tenantId == Guid.Empty) throw new InvalidOperationException("Tenant context is required for access auditing.");

        var roles = record.UserRoles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var entry = new FerpaAuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = record.UserId,
            UserRole = roles.Length == 0 ? "Unknown" : string.Join(",", roles),
            ResourceType = record.ResourceType.Length <= 40 ? record.ResourceType : record.ResourceType[..40],
            ResourceId = record.ResourceId,
            StudentId = Guid.Empty,
            Action = record.Action,
            AccessedAt = DateTime.UtcNow,
            Justified = true,
        };
        if (!string.IsNullOrWhiteSpace(record.IpAddress))
        {
            var aad = Encoding.UTF8.GetBytes($"{tenantId:N}:{entry.Id:N}:ferpa-ip");
            entry.SetEncryptedIpAddress(record.IpAddress, encryption.Encrypt(Encoding.UTF8.GetBytes(record.IpAddress), aad));
        }

        db.FerpaAuditLogs.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
