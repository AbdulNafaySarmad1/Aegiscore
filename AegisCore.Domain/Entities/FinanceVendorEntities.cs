using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceVendors")]
public sealed class FinanceVendor : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(200)] public string LegalName { get; set; } = string.Empty;
    [MaxLength(200)] public string? TradeName { get; set; }
    [Required, MaxLength(12)] public string Tier { get; set; } = "TIER3";
    [MaxLength(40)] public string? TaxId { get; set; }
    [Required, MaxLength(3)] public string InvoiceCurrency { get; set; } = "PKR";
    [Required, MaxLength(20)] public string PaymentTerms { get; set; } = "NET30";
    [MaxLength(60)] public string? Iban { get; set; }
    [MaxLength(120)] public string? BankName { get; set; }
    [MaxLength(40)] public string? Swift { get; set; }
    [MaxLength(40)] public string? RaastId { get; set; }
    [MaxLength(30)] public string Category { get; set; } = "Other";
    [MaxLength(20)] public string Status { get; set; } = "ACTIVE";
    [MaxLength(20)] public string SanctionsStatus { get; set; } = "PENDING";
    [MaxLength(500)] public string? SanctionsReference { get; set; }
    [MaxLength(1000)] public string? RelationshipNotes { get; set; }
    [Column(TypeName = "numeric(5,2)")] public decimal PerformanceScore { get; set; } = 100m;
    public DateTime? LastSanctionsScreenedAt { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FinanceVendorContact> Contacts { get; set; } = [];
    public ICollection<FinanceStandingPurchaseOrder> StandingPurchaseOrders { get; set; } = [];
}

[Table("FinanceVendorContacts")]
public sealed class FinanceVendorContact : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [MaxLength(100)] public string? Designation { get; set; }
    [MaxLength(20)] public string? Mobile { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(VendorId))] public FinanceVendor Vendor { get; set; } = null!;
}

[Table("FinanceStandingPurchaseOrders")]
public sealed class FinanceStandingPurchaseOrder : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    [Required, MaxLength(40)] public string Number { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Period { get; set; } = "MONTHLY";
    [Column(TypeName = "numeric(19,4)")] public decimal LimitAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal UsedAmount { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "ACTIVE";
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(VendorId))] public FinanceVendor Vendor { get; set; } = null!;
    public ICollection<FinanceStandingPurchaseOrderSpend> Spend { get; set; } = [];
}

[Table("FinanceStandingPurchaseOrderSpend")]
public sealed class FinanceStandingPurchaseOrderSpend : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StandingPurchaseOrderId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    public DateTime SpentAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(StandingPurchaseOrderId))] public FinanceStandingPurchaseOrder StandingPurchaseOrder { get; set; } = null!;
}
