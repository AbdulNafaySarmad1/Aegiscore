using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Rooms")]
public class Room
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? RoomNumber { get; set; }

    public Guid FloorId { get; set; }

    public int Capacity { get; set; }

    [MaxLength(100)]
    public string? RoomType { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(FloorId))]
    public Floor Floor { get; set; } = null!;
}
