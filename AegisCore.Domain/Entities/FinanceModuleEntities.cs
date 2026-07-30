using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceVouchers")]
public sealed class FinanceVoucher : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(40)] public string VoucherNumber { get; set; } = "";
    [Required, MaxLength(20)] public string VoucherType { get; set; } = "JV";
    [Required, MaxLength(30)] public string Status { get; set; } = "DRAFT";
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [MaxLength(1000)] public string? Description { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal TotalAmount { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    [MaxLength(1000)] public string? RejectionNote { get; set; }
    [MaxLength(80)] public string? PqcAlgorithm { get; set; }
    [MaxLength(200)] public string? PqcKeyId { get; set; }
    public byte[]? PqcSignature { get; set; }
    public byte[]? PqcPublicKey { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FinanceVoucherLine> Lines { get; set; } = [];
    public ICollection<FinanceVoucherApproval> Approvals { get; set; } = [];
}

[Table("FinanceVoucherSequences")]
public sealed class FinanceVoucherSequence : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(20)] public string VoucherType { get; set; } = "JV";
    public int FiscalYear { get; set; }
    public long NextNumber { get; set; } = 1;
    public Guid TenantId { get; set; }
}

[Table("FinanceVoucherLines")]
public sealed class FinanceVoucherLine : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinanceVoucherId { get; set; }
    public Guid ChartOfAccountId { get; set; }
    public Guid? CostCenterId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Debit { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Credit { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinanceVoucherId))] public FinanceVoucher Voucher { get; set; } = null!;
}

[Table("FinanceVoucherApprovals")]
public sealed class FinanceVoucherApproval : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinanceVoucherId { get; set; }
    public Guid ApproverUserId { get; set; }
    [Required, MaxLength(20)] public string Decision { get; set; } = "PENDING";
    [MaxLength(1000)] public string? Note { get; set; }
    [MaxLength(80)] public string? PqcAlgorithm { get; set; }
    [MaxLength(200)] public string? PqcKeyId { get; set; }
    public byte[]? PqcSignature { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FeeDemands")]
public sealed class FeeDemand : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    [Required, MaxLength(40)] public string DemandNumber { get; set; } = "";
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal PaidAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal OutstandingAmount { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "OPEN";
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FeeInstallment> Installments { get; set; } = [];
    public ICollection<FinancePayment> Payments { get; set; } = [];
}

[Table("FeeInstallments")]
public sealed class FeeInstallment : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FeeDemandId { get; set; }
    public int Sequence { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal PaidAmount { get; set; }
    public DateTime DueDate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "PENDING";
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FeeDemandId))] public FeeDemand Demand { get; set; } = null!;
}

[Table("FinancePayments")]
public sealed class FinancePayment : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FeeDemandId { get; set; }
    public Guid? FeeInstallmentId { get; set; }
    [Required, MaxLength(40)] public string IdempotencyKey { get; set; } = "";
    [Required, MaxLength(30)] public string Method { get; set; } = "MANUAL";
    [MaxLength(100)] public string? GatewaySlug { get; set; }
    [MaxLength(200)] public string? ExternalReference { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "PENDING";
    [MaxLength(100)] public string? WebhookId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAt { get; set; }
    public Guid TenantId { get; set; }
}

[Table("PaymentGatewayConfigurations")]
public sealed class PaymentGatewayConfiguration : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(40)] public string GatewaySlug { get; set; } = "";
    [Required, MaxLength(12)] public string Environment { get; set; } = "sandbox";
    [Required, MaxLength(300)] public string SecretNamespace { get; set; } = "";
    public bool IsActive { get; set; }
    [Required, MaxLength(20)] public string HealthStatus { get; set; } = "unknown";
    public DateTime? LastHealthCheckAt { get; set; }
    [MaxLength(500)] public string? HealthError { get; set; }
    public Guid ConfiguredByUserId { get; set; }
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("PaymentGatewayEvents")]
public sealed class PaymentGatewayEvent : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(40)] public string GatewaySlug { get; set; } = "";
    [Required, MaxLength(40)] public string EventType { get; set; } = "";
    [MaxLength(200)] public string? ReferenceId { get; set; }
    public Guid? FinancePaymentId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal? Amount { get; set; }
    [MaxLength(3)] public string? Currency { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "RECEIVED";
    public bool IsSandbox { get; set; }
    [MaxLength(500)] public string? ErrorMessage { get; set; }
    [Column(TypeName = "jsonb")] public string SanitizedPayloadJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("ExchangeRateSnapshots")]
public sealed class ExchangeRateSnapshot : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(3)] public string BaseCurrency { get; set; } = "PKR";
    [Required, MaxLength(3)] public string QuoteCurrency { get; set; } = "USD";
    [Column(TypeName = "numeric(24,12)")] public decimal Rate { get; set; }
    [Required, MaxLength(20)] public string Source { get; set; } = "SBP";
    public DateTime ValidForDate { get; set; }
    public DateTime PulledAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
