using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("DocumentTiers")]
public class DocumentTier : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [MaxLength(50)] public string Name { get; set; } = "";        // "Critical", "Important", "Standard"
    public int TierLevel { get; set; } = 1;                        // 1=Critical(50yr), 2=Important(20yr), 3=Standard(5yr)
    public int RetentionYears { get; set; } = 5;
    public int RetentionDays { get; set; } // Computed: retentionYears * 365
    [MaxLength(50)] public string ObjectLockMode { get; set; } = "GOVERNANCE"; // COMPLIANCE or GOVERNANCE
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(100)] public string? BucketName { get; set; }       // R2 bucket name for this tier
    [MaxLength(200)] public string? StoragePrefix { get; set; }    // Path prefix within bucket
    public bool RequirePqcVerification { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
}

[Table("DocumentVaultEntries")]
public class DocumentVaultEntry : ITenantScoped
{
    [Key] public Guid Id { get; set; }

    // What the document is
    [MaxLength(500)] public string FileName { get; set; } = "";
    [MaxLength(100)] public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    [MaxLength(50)] public string DocumentCategory { get; set; } = ""; // "Degree", "Transcript", "FeeChallan", "GradeAssessment", "Attendance", etc.

    // Tier assignment
    public Guid TierId { get; set; }
    public int TierLevel { get; set; }

    // PQC verification
    [MaxLength(200)] public string? PqcSignatureBase64 { get; set; }     // ML-DSA signature
    [MaxLength(200)] public string? PqcPublicKeyBase64 { get; set; }     // Verification key
    [MaxLength(128)] public string Sha512Hash { get; set; } = "";        // Content hash before signature
    public bool IsPqcVerified { get; set; }
    public DateTime? PqcVerifiedAt { get; set; }

    // Storage location
    [MaxLength(1000)] public string? StorageUrl { get; set; }
    [MaxLength(200)] public string? StorageBucket { get; set; }
    [MaxLength(500)] public string? StorageKey { get; set; }

    // Retention lock
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime RetentionExpiresAt { get; set; }             // When the lock expires
    public bool IsRetentionLocked { get; set; }                  // Object Lock applied
    [MaxLength(50)] public string? LockMode { get; set; }        // GOVERNANCE or COMPLIANCE
    public DateTime? LockedUntil { get; set; }                   // S3 retention until date
    [MaxLength(100)] public string? ObjectLockLegalHoldId { get; set; }

    // Audit
    public Guid UploadedByUserId { get; set; }
    [MaxLength(50)] public string? UploadedByRole { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletionReason { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public Guid TenantId { get; set; }
}

[Table("DocumentAccessLogs")]
public class DocumentAccessLog : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    [MaxLength(20)] public string AccessType { get; set; } = "";   // "Upload", "Download", "View", "Verify", "DeleteAttempt"
    public Guid? AccessedByUserId { get; set; }
    [MaxLength(50)] public string? AccessedByRole { get; set; }
    [MaxLength(50)] public string? IpAddress { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public bool WasDenied { get; set; }
    [MaxLength(500)] public string? DenialReason { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("DocumentRetentionPolicies")]
public class DocumentRetentionPolicy : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [MaxLength(100)] public string DocumentCategory { get; set; } = ""; // Maps categories to tiers
    public int TierLevel { get; set; }
    public int RetentionYears { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsOverride { get; set; } // true = admin overrode default tier assignment
    public Guid TenantId { get; set; }
}
