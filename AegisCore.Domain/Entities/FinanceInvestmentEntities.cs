using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceInvestmentFunds")]
public sealed class FinanceInvestmentFund : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string FundType { get; set; } = "ENDOWMENT";
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal Principal { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal CurrentValue { get; set; }
    [Column(TypeName = "numeric(9,4)")] public decimal SpendingRatePercent { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ReserveTarget { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "ACTIVE";
    public Guid TenantId { get; set; }
    public ICollection<FinanceInvestmentHolding> Holdings { get; set; } = [];
}

[Table("FinanceInvestmentHoldings")]
public sealed class FinanceInvestmentHolding : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinanceInvestmentFundId { get; set; }
    [Required, MaxLength(100)] public string InstrumentType { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Issuer { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string AssetClass { get; set; } = "FIXED_INCOME";
    [Required, MaxLength(20)] public string Measurement { get; set; } = "AMORTIZED_COST";
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal CostBasis { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal MarketValue { get; set; }
    [Column(TypeName = "numeric(9,4)")] public decimal AnnualYieldPercent { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "HELD";
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinanceInvestmentFundId))] public FinanceInvestmentFund Fund { get; set; } = null!;
    public ICollection<FinanceInvestmentTransaction> Transactions { get; set; } = [];
}

[Table("FinanceInvestmentTransactions")]
public sealed class FinanceInvestmentTransaction : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinanceInvestmentHoldingId { get; set; }
    [Required, MaxLength(20)] public string TransactionType { get; set; } = string.Empty;
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal GainOrLoss { get; set; }
    [Required, MaxLength(500)] public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinanceInvestmentHoldingId))] public FinanceInvestmentHolding Holding { get; set; } = null!;
}

[Table("FinanceInvestmentBenchmarks")]
public sealed class FinanceInvestmentBenchmark : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(80)] public string Name { get; set; } = string.Empty;
    public DateTime ObservationDate { get; set; }
    [Column(TypeName = "numeric(9,4)")] public decimal AnnualReturnPercent { get; set; }
    [Required, MaxLength(100)] public string Source { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}
