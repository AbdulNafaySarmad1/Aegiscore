namespace AegisCore.Application.Compliance;

/// <summary>
/// The compliance decisions an installation owns and may change at any time: who its data
/// protection contact is, where its data physically lives, and how long each category of record is
/// kept. None of these is a constant in the product — a supplier cannot name a university's data
/// protection officer, nor decide how long that university keeps disciplinary records. The values
/// shipped are starting points; the administrator sets the real ones and can revise them whenever
/// the institution's policy, its hosting or the law changes.
///
/// Stored as tenant configuration under the <c>compliance.*</c> keys, which is the same place the
/// retention worker reads, so a change takes effect on the next nightly run without a restart.
/// (docs/compliance/05-governance.md, 09-retention-policy.md, 03-data-residency.md.)
/// </summary>
public sealed record ComplianceSettings(
    DataProtectionContact Contact,
    DataResidency Residency,
    RetentionSettings Retention)
{
    public static ComplianceSettings Defaults => new(DataProtectionContact.Empty, DataResidency.Empty, new RetentionSettings());
}

/// <summary>
/// The named, accountable person a data subject, a client or a regulator writes to. Empty until
/// the institution fills it in: the product must not invent an accountable person, and a blank
/// contact is surfaced as an unmet obligation rather than hidden behind a placeholder.
/// </summary>
public sealed record DataProtectionContact(
    string? Name = null,
    string? Role = null,
    string? Email = null,
    string? Phone = null,
    string? PostalAddress = null,
    string? DeputyName = null,
    string? DeputyEmail = null)
{
    public static DataProtectionContact Empty => new();

    /// <summary>A contact is usable when someone can actually be reached: a name and an email.</summary>
    public bool IsComplete => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Email);

    public const string NameKey = "compliance.dpc_name";
    public const string RoleKey = "compliance.dpc_role";
    public const string EmailKey = "compliance.dpc_email";
    public const string PhoneKey = "compliance.dpc_phone";
    public const string PostalAddressKey = "compliance.dpc_postal_address";
    public const string DeputyNameKey = "compliance.dpc_deputy_name";
    public const string DeputyEmailKey = "compliance.dpc_deputy_email";
}

/// <summary>
/// Where this installation's data physically is. The product cannot know — the institution chose
/// the host — and the answer drives the cross-border transfer questions in the draft PDPA and the
/// data-sovereignty expectations of the National Data Governance Policy.
/// </summary>
public sealed record DataResidency(
    string? DeploymentModel = null,
    string? DatabaseLocation = null,
    string? FileStorageLocation = null,
    string? BackupLocation = null,
    string? ReplicaLocation = null,
    bool ApprovedForProduction = false,
    string? Notes = null)
{
    public static DataResidency Empty => new();

    public bool IsComplete => !string.IsNullOrWhiteSpace(DeploymentModel) && !string.IsNullOrWhiteSpace(DatabaseLocation);

    public const string DeploymentModelKey = "compliance.deployment_model";
    public const string DatabaseLocationKey = "compliance.database_location";
    public const string FileStorageLocationKey = "compliance.file_storage_location";
    public const string BackupLocationKey = "compliance.backup_location";
    public const string ReplicaLocationKey = "compliance.replica_location";
    public const string ApprovedForProductionKey = "compliance.residency_approved_for_production";
    public const string NotesKey = "compliance.residency_notes";
}

/// <summary>One unmet obligation, for the readiness list the compliance screen shows.</summary>
public sealed record ComplianceSettingGap(string Setting, string Why, string Document);

public interface IComplianceSettingsService
{
    Task<ComplianceSettings> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes only the values supplied; a null leaves the stored value alone, so a screen that edits
    /// one section cannot blank another. Every change is audited by the configuration centre.
    /// </summary>
    Task<ComplianceSettings> SaveAsync(Guid tenantId, ComplianceSettingsUpdate update, Guid actorId, CancellationToken cancellationToken = default);

    /// <summary>What is still unset, and which document says it matters.</summary>
    IReadOnlyList<ComplianceSettingGap> Gaps(ComplianceSettings settings);
}

public sealed record ComplianceSettingsUpdate(
    DataProtectionContact? Contact = null,
    DataResidency? Residency = null,
    RetentionSettingsUpdate? Retention = null);

/// <summary>Nullable mirror of <see cref="RetentionSettings"/>: only the periods actually supplied are written.</summary>
public sealed record RetentionSettingsUpdate(
    int? AuditRetentionYears = null,
    int? WriteAuditRetentionYears = null,
    int? LoginAuditRetentionDays = null,
    int? VisitorRetentionDays = null,
    int? ApplicantRetentionYears = null,
    int? StudentRecordRetentionYears = null,
    int? WelfareRetentionYears = null,
    int? StaffRetentionYears = null,
    int? FinancialRetentionYears = null,
    int? RecruitmentRetentionYears = null,
    IReadOnlyCollection<Guid>? LegalHoldIds = null);
