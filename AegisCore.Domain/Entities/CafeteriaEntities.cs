using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("CafeteriaUnits")]
public class CafeteriaUnit : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string? Location { get; set; }
    public int Capacity { get; set; }
    [MaxLength(100)] public string? OperatingHours { get; set; }
    public Guid? VendorId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("MenuItems")]
public class MenuItem : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid CafeteriaUnitId { get; set; }
    [Required, MaxLength(200)] public string ItemName { get; set; } = string.Empty;
    [MaxLength(50)] public string Category { get; set; } = "meal";
    [Column(TypeName = "decimal(10,2)")] public decimal Price { get; set; }
    [MaxLength(500)] public string? IngredientsCsv { get; set; }
    [MaxLength(500)] public string? AllergensCsv { get; set; }
    public int Calories { get; set; }
    public bool IsHalalCertified { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("MealPlans")]
public class MealPlan : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(200)] public string PlanName { get; set; } = string.Empty;
    [Column(TypeName = "decimal(10,2)")] public decimal DailyAllowance { get; set; }
    [MaxLength(500)] public string? EligibleItemsCsv { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("PreOrders")]
public class PreOrder : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
    [Column(TypeName = "decimal(10,2)")] public decimal TotalAmount { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "placed";
    public DateTimeOffset ReadyAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("CafeteriaTransactions")]
public class CafeteriaTransaction : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
    [Column(TypeName = "decimal(10,2)")] public decimal Amount { get; set; }
    [MaxLength(20)] public string PaymentMethod { get; set; } = "wallet";
    [Column(TypeName = "decimal(10,2)")] public decimal WalletDebit { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal MealPlanDebit { get; set; }
    public DateTimeOffset TransactionDate { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("VendorPerformanceScores")]
public class VendorPerformanceScore : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public Guid CafeteriaUnitId { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal HygieneScore { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal MenuAdherenceScore { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal ComplaintsScore { get; set; }
    [MaxLength(20)] public string OverallRating { get; set; } = "satisfactory";
    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("CafeteriaInventory")]
public class CafeteriaInventory : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid CafeteriaUnitId { get; set; }
    [Required, MaxLength(200)] public string IngredientName { get; set; } = string.Empty;
    [Column(TypeName = "decimal(10,2)")] public decimal QuantityOnHand { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal ReorderPoint { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("WasteLogs")]
public class WasteLog : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid CafeteriaUnitId { get; set; }
    public Guid MenuItemId { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal QuantityWasted { get; set; }
    [MaxLength(100)] public string? Reason { get; set; }
    public DateTimeOffset LoggedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
