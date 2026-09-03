using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("SensorReadings")]
public class SensorReading : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid SensorId { get; set; }

    [Column(TypeName = "decimal(12,4)")]
    public decimal Value { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "jsonb")]
    public string? ExtraDataJson { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(SensorId))]
    public IoTSensor Sensor { get; set; } = null!;
}
