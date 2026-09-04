using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("BackupConfigurations")]
public class BackupConfiguration : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public BackupType BackupType { get; set; } = BackupType.Full;

    [MaxLength(100)]
    public string CronExpression { get; set; } = "0 0 2 * * ?";

    public bool IsEnabled { get; set; } = true;

    public int RetentionDaysHot { get; set; } = 30;

    public int RetentionDaysCold { get; set; } = 365;

    public bool EncryptBackup { get; set; } = true;

    [MaxLength(500)]
    public string StoragePath { get; set; } = "/backups";

    [MaxLength(100)]
    public string? DatabaseName { get; set; }

    public bool VerifyAfterBackup { get; set; }

    public DateTime? LastRunAt { get; set; }

    public DateTime? NextRunAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
