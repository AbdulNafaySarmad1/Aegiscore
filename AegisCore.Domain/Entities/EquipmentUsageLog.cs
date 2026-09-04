using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("EquipmentUsageLogs")]
public class EquipmentUsageLog : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid LabEquipmentId { get; set; }

    [ForeignKey(nameof(LabEquipmentId))]
    public LabEquipment LabEquipment { get; set; } = null!;

    public Guid UsedByUserId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
