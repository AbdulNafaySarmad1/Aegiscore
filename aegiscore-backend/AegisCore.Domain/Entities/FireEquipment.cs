using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("FireEquipments")]
public class FireEquipment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EquipmentCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string EquipmentType { get; set; } = string.Empty;

    public Guid? BuildingId { get; set; }

    [ForeignKey(nameof(BuildingId))]
    public Building? Building { get; set; }

    public Guid? RoomId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public Room? Room { get; set; }

    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    public DateTime? LastInspectionDate { get; set; }

    public DateTime? NextInspectionDue { get; set; }

    public FireEquipmentStatus Status { get; set; } = FireEquipmentStatus.Active;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<FireDrill> Drills { get; set; } = new List<FireDrill>();
}
