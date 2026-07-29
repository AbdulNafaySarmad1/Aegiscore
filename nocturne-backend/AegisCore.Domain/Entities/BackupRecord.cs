using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("BackupRecords")]
public class BackupRecord
{
    [Key]
    public Guid Id { get; set; }

    public Guid ConfigurationId { get; set; }

    public BackupType BackupType { get; set; }

    public BackupStatus Status { get; set; } = BackupStatus.Running;

    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    [MaxLength(100)]
    public string? Checksum { get; set; }

    public bool IsEncrypted { get; set; }

    public BackupRetentionTier RetentionTier { get; set; } = BackupRetentionTier.Hot;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    [MaxLength(100)]
    public string? TriggeredBy { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(ConfigurationId))]
    public BackupConfiguration Configuration { get; set; } = null!;
}
