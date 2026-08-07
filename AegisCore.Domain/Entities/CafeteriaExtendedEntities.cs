using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// Extended cafeteria entities preserved from finance-onboarding-foundation

[Table("FoodOrders")] public class FoodOrder { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid MenuItemId { get; set; } public Guid CafeteriaUnitId { get; set; }
    public int Quantity { get; set; } = 1; [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    public FoodOrderStatus Status { get; set; } = FoodOrderStatus.Placed;
    [MaxLength(50)] public string? PaymentMethod { get; set; } [MaxLength(100)] public string? TransactionRef { get; set; }
    public bool IsPreOrder { get; set; } public DateTime? EstimatedReadyAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("InventoryItems")] public class InventoryItem { [Key] public Guid Id { get; set; }
    public Guid CafeteriaUnitId { get; set; } [MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(50)] public string? Unit { get; set; } public double QuantityOnHand { get; set; }
    public double ReorderPoint { get; set; } public double ReorderQuantity { get; set; }
    public DateTime? ExpiryDate { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal UnitCost { get; set; }
    public Guid TenantId { get; set; } }

[Table("InventoryAlerts")] public class InventoryAlert { [Key] public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; } public InventoryAlertType Type { get; set; }
    [MaxLength(500)] public string? Message { get; set; } public bool IsResolved { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("VendorScorecards")] public class VendorScorecard { [Key] public Guid Id { get; set; }
    public Guid VendorId { get; set; } [MaxLength(20)] public string Month { get; set; } = "";
    public VendorPerformance Rating { get; set; } public double FoodQualityScore { get; set; }
    public double HygieneScore { get; set; } public double ServiceScore { get; set; }
    public int ComplaintCount { get; set; } public bool HalalCertValid { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("DemandForecasts")] public class DemandForecast { [Key] public Guid Id { get; set; }
    public Guid MenuItemId { get; set; } public Guid CafeteriaUnitId { get; set; }
    public DateTime ForecastDate { get; set; } public int PredictedQuantity { get; set; }
    public int ActualQuantity { get; set; } public double Accuracy { get; set; }
    [MaxLength(200)] public string? Notes { get; set; } public Guid TenantId { get; set; } }
