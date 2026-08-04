using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Floors")]
public class Floor
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int FloorNumber { get; set; }

    public Guid BuildingId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(BuildingId))]
    public Building Building { get; set; } = null!;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
