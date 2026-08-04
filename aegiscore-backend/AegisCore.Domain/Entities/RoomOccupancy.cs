using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("RoomOccupancies")]
public class RoomOccupancy
{
    [Key]
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public OccupancyState State { get; set; } = OccupancyState.Vacant;

    public int? CurrentOccupants { get; set; }

    public int? MaxCapacity { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? CurrentEvent { get; set; }

    public DateTime? BookingUntil { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(RoomId))]
    public Room Room { get; set; } = null!;
}
