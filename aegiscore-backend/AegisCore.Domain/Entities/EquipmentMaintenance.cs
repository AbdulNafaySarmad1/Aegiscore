using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("EquipmentMaintenances")]
public class EquipmentMaintenance
{
    [Key]
    public Guid Id { get; set; }

    public Guid LabEquipmentId { get; set; }

    [ForeignKey(nameof(LabEquipmentId))]
    public LabEquipment LabEquipment { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string IssueDescription { get; set; } = string.Empty;

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    public Guid ReportedByUserId { get; set; }

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Reported;

    public Guid? AssignedToUserId { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(500)]
    public string ResolutionNotes { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Cost { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
