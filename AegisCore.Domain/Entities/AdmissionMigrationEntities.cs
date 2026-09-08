using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

/// <summary>
/// Per-program, per-intake roll-number counter. Incremented under an optimistic
/// concurrency check so concurrent admissions never collide on a sequence value.
/// </summary>
[Table("ProgramIntakeSequences")]
public class ProgramIntakeSequence : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public Guid IntakeId { get; set; }
    public Guid ProgramId { get; set; }

    public int NextValue { get; set; } = 1;

    /// <summary>Bumped on every increment; used as an EF concurrency token.</summary>
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Per-tenant institutional email provisioning configuration. The mode is admin-selected;
/// all three modes (Google Workspace / Microsoft 365 / Manual) are implemented.
/// </summary>
[Table("EmailProvisioningConfigs")]
public class EmailProvisioningConfig : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public EmailProvisioningMode Mode { get; set; } = EmailProvisioningMode.Manual;

    [MaxLength(255)] public string? DomainName { get; set; }

    /// <summary>Encrypted provider credentials JSON (service account / app registration). Never returned to clients.</summary>
    [Column(TypeName = "text")] public string? CredentialsCipher { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
}
