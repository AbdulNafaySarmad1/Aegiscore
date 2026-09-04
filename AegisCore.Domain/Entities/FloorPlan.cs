using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("FloorPlans")]
public class FloorPlan : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid FloorId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string RoomsLayoutJson { get; set; } = "[]";

    public int ImageWidth { get; set; }

    public int ImageHeight { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(FloorId))]
    public Floor Floor { get; set; } = null!;
}
