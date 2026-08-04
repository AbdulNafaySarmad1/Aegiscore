using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinancePayrollBankInstructions")]
public sealed class FinancePayrollBankInstruction : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    [Required] public byte[] EncryptedAccountReference { get; set; } = [];
    [Required, MaxLength(30)] public string RoutingCode { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string AccountName { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FinancePayrollPaymentFiles")]
public sealed class FinancePayrollPaymentFile : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid PayrollPostingId { get; set; }
    [Required, MaxLength(20)] public string Format { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string StorageKey { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string Sha256 { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string PqcAlgorithm { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string PqcKeyId { get; set; } = string.Empty;
    [Required] public byte[] PqcSignature { get; set; } = [];
    [Column(TypeName = "numeric(19,4)")] public decimal TotalAmount { get; set; }
    public int RecordCount { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Guid GeneratedByUserId { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceGratuityAssumptions")]
public sealed class FinanceGratuityAssumption : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Column(TypeName = "numeric(9,6)")] public decimal DiscountRatePercent { get; set; }
    [Column(TypeName = "numeric(9,6)")] public decimal SalaryGrowthRatePercent { get; set; }
    [Column(TypeName = "numeric(9,6)")] public decimal TurnoverRatePercent { get; set; }
    [Column(TypeName = "numeric(9,4)")] public decimal BenefitDivisor { get; set; } = 30m;
    public int RetirementHorizonYears { get; set; } = 20;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceGratuityValuations")]
public sealed class FinanceGratuityValuation : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid AssumptionId { get; set; }
    public DateTime ValuationDate { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal DefinedBenefitObligation { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal CurrentServiceCost { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal InterestCost { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid TenantId { get; set; }
}
