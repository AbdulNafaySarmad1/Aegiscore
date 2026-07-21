using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ExtensionInstallations")]
public class ExtensionInstallation
{
    [Key]
    public Guid Id { get; set; }

    public Guid ExtensionId { get; set; }

    [Required]
    [MaxLength(50)]
    public string InstalledVersion { get; set; } = "1.0.0";

    public InstallationStatus Status { get; set; } = InstallationStatus.Installing;

    [Column(TypeName = "jsonb")]
    public string ConfigJson { get; set; } = "{}";

    [MaxLength(2000)]
    public string? InstallLog { get; set; }

    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    public DateTime? UninstalledAt { get; set; }

    public Guid InstalledByUserId { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(ExtensionId))]
    public Extension Extension { get; set; } = null!;
}
