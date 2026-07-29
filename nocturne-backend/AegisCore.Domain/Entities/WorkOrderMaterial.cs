using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("WorkOrderMaterials")]
public class WorkOrderMaterial
{
    [Key]
    public Guid Id { get; set; }

    public Guid WorkOrderId { get; set; }

    [ForeignKey(nameof(WorkOrderId))]
    public WorkOrder WorkOrder { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string MaterialName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
