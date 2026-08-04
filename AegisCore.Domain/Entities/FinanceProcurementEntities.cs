using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinancePurchaseOrders")]
public sealed class FinancePurchaseOrder : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    [Required, MaxLength(50)] public string Number { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal TotalAmount { get; set; }
    [Required, MaxLength(24)] public string Status { get; set; } = "DRAFT";
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FinancePurchaseOrderLine> Lines { get; set; } = [];
}

[Table("FinancePurchaseOrderLines")]
public sealed class FinancePurchaseOrderLine : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ExpenseAccountId { get; set; }
    [Required, MaxLength(500)] public string Description { get; set; } = string.Empty;
    [Column(TypeName = "numeric(19,4)")] public decimal Quantity { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal LineTotal { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceGoodsReceipts")]
public sealed class FinanceGoodsReceipt : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    [Required, MaxLength(50)] public string Number { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Status { get; set; } = "CONFIRMED";
    public Guid ConfirmedByUserId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FinanceGoodsReceiptLine> Lines { get; set; } = [];
}

[Table("FinanceGoodsReceiptLines")]
public sealed class FinanceGoodsReceiptLine : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal QuantityAccepted { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal QuantityRejected { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceVendorInvoices")]
public sealed class FinanceVendorInvoice : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    [Required, MaxLength(100)] public string InvoiceNumber { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal TotalAmount { get; set; }
    [Required, MaxLength(24)] public string Status { get; set; } = "RECEIVED";
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public Guid? PaymentVoucherId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    public ICollection<FinanceVendorInvoiceLine> Lines { get; set; } = [];
}

[Table("FinanceVendorInvoiceLines")]
public sealed class FinanceVendorInvoiceLine : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VendorInvoiceId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Quantity { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal LineTotal { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceThreeWayMatches")]
public sealed class FinanceThreeWayMatch : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VendorInvoiceId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal PurchaseOrderTotal { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ReceivedTotal { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal InvoiceTotal { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Variance { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "FAILED";
    public Guid MatchedByUserId { get; set; }
    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FinanceVendorContracts")]
public sealed class FinanceVendorContract : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    [Required, MaxLength(100)] public string ContractNumber { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string Title { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public int RenewalNoticeDays { get; set; } = 30;
    [Column(TypeName = "numeric(5,2)")] public decimal SlaScore { get; set; } = 100m;
    [Required, MaxLength(20)] public string Status { get; set; } = "ACTIVE";
    public Guid TenantId { get; set; }
}

[Table("FinanceVendorDocuments")]
public sealed class FinanceVendorDocument : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    [Required, MaxLength(60)] public string DocumentType { get; set; } = string.Empty;
    [Required, MaxLength(260)] public string FileName { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string StorageKey { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string Sha256 { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string PqcAlgorithm { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string PqcKeyId { get; set; } = string.Empty;
    [Required] public byte[] PqcSignature { get; set; } = [];
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
