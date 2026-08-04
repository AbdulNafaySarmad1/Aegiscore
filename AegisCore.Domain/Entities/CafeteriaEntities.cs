using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// OPTIONAL-26 Cafeteria Management (FoodCore)
[Table("CafeteriaUnits")] public class CafeteriaUnit { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(200)] public string? Location { get; set; }
    public int Capacity { get; set; } [MaxLength(200)] public string? OperatingHours { get; set; }
    public Guid? VendorId { get; set; } public bool IsActive { get; set; } = true; public Guid TenantId { get; set; } }

[Table("MenuItems")] public class MenuItem { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public MenuItemCategory Category { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
    [MaxLength(2000)] public string? Description { get; set; } [Column(TypeName = "jsonb")] public string IngredientsJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string AllergensJson { get; set; } = "[]";
    public int Calories { get; set; } public bool IsHalal { get; set; } = true;
    public bool IsAvailable { get; set; } = true; [MaxLength(500)] public string? ImageUrl { get; set; }
    public Guid? CafeteriaUnitId { get; set; } public Guid TenantId { get; set; } }

[Table("MealPlans")] public class MealPlan { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public MealPlanType Type { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DailyAllowance { get; set; }
    [Column(TypeName = "jsonb")] public string EligibleItemsJson { get; set; } = "[]";
    public Guid? StudentId { get; set; } public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } public DateTime? EffectiveTo { get; set; } public Guid TenantId { get; set; } }

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

[Table("WasteLogs")] public class WasteLog { [Key] public Guid Id { get; set; }
    public Guid CafeteriaUnitId { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal Quantity { get; set; }
    [MaxLength(50)] public string? Unit { get; set; } [MaxLength(200)] public string? Reason { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal EstimatedCost { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// Cross-cutting: Personality Mode
[Table("PersonalityConfigs")] public class PersonalityConfig { [Key] public Guid Id { get; set; }
    public bool IsEnabled { get; set; } public bool IsLockedOff { get; set; }
    public Guid? UserId { get; set; } public Guid? RoleId { get; set; }
    public bool IsActiveForUser { get; set; } public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// Cross-cutting: Chart System
[Table("ChartConfigurations")] public class ChartConfiguration { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public ChartType Type { get; set; }
    [MaxLength(500)] public string? DataSourceKey { get; set; } [Column(TypeName = "jsonb")] public string ConfigJson { get; set; } = "{}";
    [MaxLength(200)] public string? InsightI18nKey { get; set; } public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("InsightChips")] public class InsightChip { [Key] public Guid Id { get; set; }
    public Guid ChartConfigurationId { get; set; } public InsightType Type { get; set; }
    [MaxLength(500)] public string Text { get; set; } = ""; [MaxLength(200)] public string? I18nKey { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// Cross-cutting: OCR Integration
[Table("OcrProviderConfigs")] public class OcrProviderConfig { [Key] public Guid Id { get; set; }
    [MaxLength(50)] public string Provider { get; set; } = "tesseract"; [MaxLength(500)] public string? ApiEndpoint { get; set; }
    [MaxLength(500)] public string? CredentialRef { get; set; } public bool IsActive { get; set; }
    public int Priority { get; set; } = 1; public Guid TenantId { get; set; } }
