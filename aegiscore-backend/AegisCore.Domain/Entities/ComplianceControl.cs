using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ComplianceControls")]
public class ComplianceControl
{
    [Key]
    public Guid Id { get; set; }

    public Guid FrameworkId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ControlCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public ControlStatus Status { get; set; } = ControlStatus.InProgress;

    public Guid? OwnerUserId { get; set; }

    [MaxLength(200)]
    public string? OwnerName { get; set; }

    public ControlReviewFrequency ReviewFrequency { get; set; } = ControlReviewFrequency.Annually;

    public DateTime? LastReviewedAt { get; set; }

    public DateTime? NextReviewDate { get; set; }

    [Column(TypeName = "jsonb")]
    public string AutomatedCheckConfigJson { get; set; } = "{}";

    public bool IsAutomated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(FrameworkId))]
    public ComplianceFramework Framework { get; set; } = null!;

    public ICollection<ControlEvidence> Evidences { get; set; } = new List<ControlEvidence>();
    public ICollection<AuditFinding> Findings { get; set; } = new List<AuditFinding>();
}
