using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Buildings")]
public class Building
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? BuildingCode { get; set; }

    public Guid CampusId { get; set; }

    public int FloorCount { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(CampusId))]
    public Campus Campus { get; set; } = null!;

    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
}
