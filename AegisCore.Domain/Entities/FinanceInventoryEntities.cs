using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceInventoryItems")]
public sealed class FinanceInventoryItem : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(60)] public string ItemCode { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string ItemName { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Unit { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string CostMethod { get; set; } = "WEIGHTED_AVERAGE";
    [Column(TypeName = "numeric(19,4)")] public decimal OnHandQuantity { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal WeightedAverageCost { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ReorderLevel { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ReorderQuantity { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "ACTIVE";
    public Guid TenantId { get; set; }
    public ICollection<FinanceInventoryMovement> Movements { get; set; } = [];
}

[Table("FinanceInventoryMovements")]
public sealed class FinanceInventoryMovement : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinanceInventoryItemId { get; set; }
    [Required, MaxLength(30)] public string MovementType { get; set; } = string.Empty;
    [Column(TypeName = "numeric(19,4)")] public decimal Quantity { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal UnitCost { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal TotalCost { get; set; }
    public Guid? CostCenterId { get; set; }
    [MaxLength(160)] public string? Reference { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public Guid? JournalEntryId { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinanceInventoryItemId))] public FinanceInventoryItem Item { get; set; } = null!;
}
