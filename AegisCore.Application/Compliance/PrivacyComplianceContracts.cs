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
    Task<int> ApplyRetentionAsync(Guid tenantId, DateTime nowUtc, RetentionSettings settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Retention periods per tenant (docs/compliance/09-retention-policy.md), read from the
/// <c>compliance.*</c> configuration keys; <see cref="LegalHolds"/> lists record ids the worker
/// must not delete while an investigation, appeal or lawful request is open.
/// </summary>
public sealed record RetentionSettings(
    int AuditRetentionYears = 6,
    int WriteAuditRetentionYears = 6,
    int LoginAuditRetentionDays = 365,
    int VisitorRetentionDays = 90,
    int ApplicantRetentionYears = 2,
    IReadOnlySet<Guid>? LegalHoldIds = null,
    // Periods the institution sets for the record categories the worker does not yet purge on its
    // own. They are stored and shown so the published retention notice states what the institution
    // actually decided rather than what the supplier guessed; the enforcing mechanism for each is
    // named in docs/compliance/09-retention-policy.md §2.
    int StudentRecordRetentionYears = 7,
    int WelfareRetentionYears = 7,
    int StaffRetentionYears = 7,
    int FinancialRetentionYears = 10,
    int RecruitmentRetentionYears = 1)
{
    public IReadOnlySet<Guid> LegalHolds => LegalHoldIds ?? new HashSet<Guid>();

    public const string AuditYearsKey = "compliance.audit_retention_years";
    public const string WriteAuditYearsKey = "compliance.write_audit_retention_years";
    public const string LoginAuditDaysKey = "compliance.login_audit_retention_days";
    public const string VisitorDaysKey = "compliance.visitor_retention_days";
    public const string ApplicantYearsKey = "compliance.applicant_retention_years";
    public const string LegalHoldsKey = "compliance.legal_holds";
    public const string StudentRecordYearsKey = "compliance.student_record_retention_years";
    public const string WelfareYearsKey = "compliance.welfare_retention_years";
    public const string StaffYearsKey = "compliance.staff_retention_years";
    public const string FinancialYearsKey = "compliance.financial_retention_years";
    public const string RecruitmentYearsKey = "compliance.recruitment_retention_years";

    public static readonly string[] Keys =
    [
        AuditYearsKey, WriteAuditYearsKey, LoginAuditDaysKey, VisitorDaysKey, ApplicantYearsKey, LegalHoldsKey,
        StudentRecordYearsKey, WelfareYearsKey, StaffYearsKey, FinancialYearsKey, RecruitmentYearsKey,
    ];
}
