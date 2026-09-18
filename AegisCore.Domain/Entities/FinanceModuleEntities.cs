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
    public Guid? ReportingEntityId { get; set; }
    public Guid? CounterpartyReportingEntityId { get; set; }
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
    [Required, MaxLength(100)] public string ApproverRole { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Decision { get; set; } = "PENDING";
    [MaxLength(1000)] public string? Note { get; set; }
    [MaxLength(80)] public string? PqcAlgorithm { get; set; }
    [MaxLength(200)] public string? PqcKeyId { get; set; }
    public byte[]? PqcSignature { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FinanceVoucherApprovalRules")]
public sealed class FinanceVoucherApprovalRule : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal MinimumAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal? MaximumAmount { get; set; }
    [Required, Column(TypeName = "jsonb")] public string RequiredRolesJson { get; set; } = "[]";
    public int MinimumApprovals { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
}

[Table("FinanceVoucherAttachments")]
public sealed class FinanceVoucherAttachment : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinanceVoucherId { get; set; }
    [Required, MaxLength(260)] public string FileName { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string ContentType { get; set; } = "application/octet-stream";
    [Required, MaxLength(500)] public string StorageKey { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string Sha256 { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FinanceVoucherReversals")]
public sealed class FinanceVoucherReversal : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid OriginalVoucherId { get; set; }
    public Guid ReversalVoucherId { get; set; }
    [Required, MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    public Guid RequestedByUserId { get; set; }
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
    // The demand is the single fee ledger: what the v1 StudentFeeDue carried lives here now.
    public Guid? SemesterId { get; set; }
    public Guid? FeeStructureId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ScholarshipDeduction { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal WaivedAmount { get; set; }
    public Guid? WaivedBy { get; set; }
    [MaxLength(500)] public string? WaivedReason { get; set; }
    public Guid TenantId { get; set; }
    public FinancePaymentPlan? PaymentPlan { get; set; }
    public ICollection<FeeInstallment> Installments { get; set; } = [];
    public ICollection<FinancePayment> Payments { get; set; } = [];
}

[Table("FinancePaymentPlanConfigurations")]
public sealed class FinancePaymentPlanConfiguration : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(20)] public string PlanType { get; set; } = "full";
    public bool Enabled { get; set; } = true;
    [Column(TypeName = "numeric(9,4)")] public decimal SurchargePercent { get; set; }
    [Column(TypeName = "numeric(9,4)")] public decimal MinimumDownPaymentPercent { get; set; }
    public int MaximumDuration { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal LatePenaltyAmount { get; set; }
    [Column(TypeName = "numeric(9,4)")] public decimal LatePenaltyPercent { get; set; }
    public int MaximumMissedInstallments { get; set; } = 2;
    public bool RequiresApproval { get; set; }
    [Required] public Guid TenantId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("FinancePaymentPlans")]
public sealed class FinancePaymentPlan : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid FeeDemandId { get; set; }
    [Required, MaxLength(20)] public string PlanType { get; set; } = "full";
    [Required, MaxLength(20)] public string Status { get; set; } = "PENDING_APPROVAL";
    [Column(TypeName = "numeric(19,4)")] public decimal Subtotal { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal SurchargeAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public DateTime StartDate { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int MissedInstallments { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FeeDemandId))] public FeeDemand Demand { get; set; } = null!;
}

[Table("FeeInstallments")]
public sealed class FeeInstallment : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FeeDemandId { get; set; }
    public int Sequence { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal PaidAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal PenaltyAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal RecognizedAmount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? LastPenaltyAppliedOn { get; set; }
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

[Table("FinanceReportArtifacts")]
public sealed class FinanceReportArtifact : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(50)] public string ReportType { get; set; } = string.Empty;
    [Required, MaxLength(10)] public string Format { get; set; } = "JSON";
    [Required, MaxLength(120)] public string PeriodLabel { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string StorageKey { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string ContentHash { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string PqcAlgorithm { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string PqcKeyId { get; set; } = string.Empty;
    public byte[] PqcSignature { get; set; } = [];
    public byte[] PqcPublicKey { get; set; } = [];
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Guid GeneratedByUserId { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceReportProfiles")]
public sealed class FinanceReportProfile : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string TargetStandard { get; set; } = "IFRS";
    [MaxLength(160)] public string? FunderName { get; set; }
    [Required, MaxLength(20)] public string FundFilter { get; set; } = "ALL";
    [Required, MaxLength(20)] public string PeriodType { get; set; } = "FISCAL_YEAR";
    [Required, MaxLength(3)] public string ReportCurrency { get; set; } = "PKR";
    [Required, MaxLength(20)] public string RateMethod { get; set; } = "CLOSING";
    [Required, MaxLength(20)] public string Schedule { get; set; } = "ON_DEMAND";
    [Required, MaxLength(20)] public string Delivery { get; set; } = "IN_APP";
    [Required, MaxLength(10)] public string Format { get; set; } = "JSON";
    public int ComparativeYears { get; set; } = 1;
    public bool BudgetVsActual { get; set; }
    public bool RestrictedFundUtilisation { get; set; }
    public bool GrantDrawdown { get; set; }
    public bool PqcEnabled { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public Guid TenantId { get; set; }
}

[Table("TenantFinanceConfigurations")]
public sealed class TenantFinanceConfiguration : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(20)] public string PrimaryStandard { get; set; } = "IFRS";
    public int FiscalYearStartMonth { get; set; } = 1;
    [Required, MaxLength(30)] public string FiscalYearLabel { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string BaseCurrency { get; set; } = "PKR";
    [Required, Column(TypeName = "jsonb")] public string AdditionalCurrenciesJson { get; set; } = "[]";
    [Required, MaxLength(30)] public string RateSource { get; set; } = "SBP";
    [Column(TypeName = "numeric(19,4)")] public decimal LibraryCapitalizationThreshold { get; set; } = 5000m;
    [MaxLength(300)] public string? RateApiKeySecretName { get; set; }
    [Required, Column(TypeName = "jsonb")] public string TaxSettingsJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FinancePqcKeys")]
public sealed class FinancePqcKey : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(50)] public string Purpose { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Algorithm { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string KeyId { get; set; } = string.Empty;
    [Required, MaxLength(400)] public string SecretNamespace { get; set; } = string.Empty;
    [Required] public byte[] PublicKey { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinancePqcKeyCeremonies")]
public sealed class FinancePqcKeyCeremony : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(50)] public string Purpose { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string Algorithm { get; set; } = string.Empty;
    public int RequiredApprovals { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "PENDING";
    public Guid RequestedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    [MaxLength(200)] public string? ActivatedKeyId { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FinancePqcKeyCeremonyApproval> Approvals { get; set; } = [];
}

[Table("FinancePqcKeyCeremonyApprovals")]
public sealed class FinancePqcKeyCeremonyApproval : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid CeremonyId { get; set; }
    public Guid ApprovedByUserId { get; set; }
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    [Required] public byte[] ApprovalDigest { get; set; } = [];
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(CeremonyId))] public FinancePqcKeyCeremony Ceremony { get; set; } = null!;
}

[Table("FinanceLedgerSignatures")]
public sealed class FinanceLedgerSignature : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    [Required, MaxLength(80)] public string Algorithm { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string KeyId { get; set; } = string.Empty;
    [Required] public byte[] Signature { get; set; } = [];
    [Required] public byte[] PublicKey { get; set; } = [];
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FinanceSecurityDeposits")]
public sealed class FinanceSecurityDeposit : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    [Required, MaxLength(40)] public string DepositType { get; set; } = "general";
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal OriginalAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal RefundedAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ForfeitedAmount { get; set; }
    [Required, MaxLength(24)] public string Status { get; set; } = "HELD";
    public int RefundSlaDays { get; set; } = 30;
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RefundRequestedAt { get; set; }
    public DateTime? AssessedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    [MaxLength(1000)] public string? ResolutionReason { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FinanceSecurityDepositDeduction> Deductions { get; set; } = [];
}

[Table("FinanceSecurityDepositDeductions")]
public sealed class FinanceSecurityDepositDeduction : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid SecurityDepositId { get; set; }
    [Required, MaxLength(40)] public string Category { get; set; } = "other";
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(SecurityDepositId))] public FinanceSecurityDeposit Deposit { get; set; } = null!;
}
