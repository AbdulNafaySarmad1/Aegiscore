using AegisCore.Domain.Entities;

namespace AegisCore.Application.Compliance;

public sealed record FerpaAccessRequest(
    Guid UserId,
    IReadOnlyList<string> UserRoles,
    string? IpAddress,
    string ResourceType,
    string IdentifierName,
    Guid Identifier,
    string Action);

public sealed record FerpaAccessDecision(bool IsJustified, int AuditEntriesWritten);

public sealed record DsarArchive(byte[] Content, DateTime ExpiresAt);

public interface IFerpaAuditService
{
    Task<FerpaAccessDecision> AuthorizeAndAuditAsync(FerpaAccessRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// A read of a sensitive non-student record (staff, payroll, welfare, health, foreign-student
/// status). Authorisation has already been decided by the permission layer; this only records
/// who looked, at what, from where. Compliance control AC-50.
/// </summary>
public sealed record AccessAuditRecord(
    Guid UserId,
    IReadOnlyList<string> UserRoles,
    string? IpAddress,
    string ResourceType,
    Guid ResourceId,
    string Action);

public interface IAccessAuditService
{
    Task RecordAsync(AccessAuditRecord record, CancellationToken cancellationToken = default);
}

public interface IPrivacyComplianceService
{
    Task<ConsentLedger> RecordConsentAsync(
        Guid userId,
        string dataCategory,
        string consentText,
        DateTime? expiresAt,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task RevokeConsentAsync(Guid userId, Guid consentId, CancellationToken cancellationToken = default);
    Task<ErasureRequest> RequestErasureAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ProcessPendingErasureRequestsAsync(CancellationToken cancellationToken = default);
    Task ProcessErasureAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<DsarArchive> CreateDsarArchiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> ApplyRetentionAsync(Guid tenantId, DateTime nowUtc, int auditRetentionYears, CancellationToken cancellationToken = default);
}
