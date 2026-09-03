using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("EnergyReadings")]
public class EnergyReading : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid? BuildingId { get; set; }

    public Guid? SensorId { get; set; }

    public EnergyReadingType ReadingType { get; set; } = EnergyReadingType.Electricity;

    [Column(TypeName = "decimal(14,4)")]
    public decimal Consumption { get; set; }

    [MaxLength(20)]
    public string? Unit { get; set; } = "kWh";

    public DateTime ReadingDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(14,4)")]
    public decimal? Cost { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(BuildingId))]
    public Building? Building { get; set; }
}

public enum EnergyReadingType
{
    Electricity = 1,
    Water = 2,
    Gas = 3,
    Solar = 4
}
