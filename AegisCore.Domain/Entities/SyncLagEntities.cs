using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("SyncLagConfigs")]
public class SyncLagConfig : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public int DefaultGracePeriodSeconds { get; set; } = 300; // 5 min default, IT sets any value
    public int MaxGracePeriodSeconds { get; set; } = 86400;    // configurable to any value (default 24h)
    public bool AutoCommitOnGraceExpiry { get; set; } = true;
    public bool RequireAdminApprovalForCorrections { get; set; } = true;
    [MaxLength(50)] public string CorrectionPermission { get; set; } = "AdminOnly"; // AdminOnly | OwnerOnly | Anyone
    public bool NotifyOwnerOnCorrection { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
    public Guid TenantId { get; set; }
}

[Table("SyncLagEntries")]
public class SyncLagEntry : ITenantScoped
{
    [Key] public Guid Id { get; set; }

    // What was written
    public Guid ReplicaId { get; set; }
    [MaxLength(50)] public string EntityType { get; set; } = "";  // "StudentGrade", "StudentFee", "Attendance", etc.
    public Guid EntityId { get; set; }                             // PK of the entity that was modified
    [MaxLength(100)] public string? FieldName { get; set; }        // "ObtainedMarks", "Amount", etc. (null = whole entity)

    // Before/After for audit and correction
    [MaxLength(500)] public string? OriginalValue { get; set; }    // What it was before
    [MaxLength(500)] public string? WrittenValue { get; set; }     // What replica wrote (being synced)

    // Sync status
    [MaxLength(30)] public string Status { get; set; } = "PendingSync"; // PendingSync | Syncing | Committed | Corrected | Reverted

    // Grace period
    public DateTime WrittenAt { get; set; } = DateTime.UtcNow;
    public DateTime GraceExpiresAt { get; set; }                   // After this, auto-commit locks it
    public DateTime? CommittedAt { get; set; }                     // When it was synced to central
    public DateTime? CorrectedAt { get; set; }                     // If corrected before commit

    // Who did it
    public Guid WrittenByUserId { get; set; }
    public Guid? CorrectedByUserId { get; set; }
    [MaxLength(500)] public string? CorrectionReason { get; set; }

    // Lock — once committed, no more corrections
    public bool IsLocked { get; set; }
    public DateTime? LockedAt { get; set; }

    public Guid TenantId { get; set; }
}
