using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("LabConsumables")]
public class LabConsumable
{
    [Key]
    public Guid Id { get; set; }

    public Guid ResearchLabId { get; set; }

    [ForeignKey(nameof(ResearchLabId))]
    public ResearchLab ResearchLab { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    public decimal QuantityInStock { get; set; }

    public decimal MinimumStockLevel { get; set; }

    public decimal ReorderQuantity { get; set; }

    [MaxLength(100)]
    public string StorageLocation { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }

    public bool IsHazardous { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<ConsumableRequisition> Requisitions { get; set; } = new List<ConsumableRequisition>();
}
