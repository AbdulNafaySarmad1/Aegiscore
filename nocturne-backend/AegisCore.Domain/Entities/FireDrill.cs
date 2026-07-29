using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("FireDrills")]
public class FireDrill
{
    [Key]
    public Guid Id { get; set; }

    public Guid? FireEquipmentId { get; set; }

    [ForeignKey(nameof(FireEquipmentId))]
    public FireEquipment? FireEquipment { get; set; }

    public DateTime DrillDate { get; set; }

    [MaxLength(100)]
    public string DrillType { get; set; } = string.Empty;

    public int ParticipantsCount { get; set; }

    public decimal EvacuationTimeMinutes { get; set; }

    [MaxLength(500)]
    public string Observations { get; set; } = string.Empty;

    public bool IsSuccessful { get; set; }

    public Guid ConductedByUserId { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
