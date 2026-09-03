using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("IoTSensors")]
public class IoTSensor : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? HardwareId { get; set; }

    public SensorType SensorType { get; set; }

    public SensorStatus Status { get; set; } = SensorStatus.Online;

    public Guid? BuildingId { get; set; }

    public Guid? RoomId { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [MaxLength(500)]
    public string? LocationDescription { get; set; }

    [Column(TypeName = "jsonb")]
    public string ConfigJson { get; set; } = "{}";

    public DateTime? LastReadingAt { get; set; }

    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
