using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ComplianceFrameworks")]
public class ComplianceFramework
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Version { get; set; } = "1.0";

    public ComplianceFrameworkStatus Status { get; set; } = ComplianceFrameworkStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastReviewedAt { get; set; }

    public Guid TenantId { get; set; }

    public ICollection<ComplianceControl> Controls { get; set; } = new List<ComplianceControl>();
}
