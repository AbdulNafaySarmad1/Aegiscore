using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinancePettyCashFunds")]
public sealed class FinancePettyCashFund : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid CustodianUserId { get; set; }
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal FloatAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    public ICollection<FinancePettyCashExpense> Expenses { get; set; } = [];
    public ICollection<FinancePettyCashCount> Counts { get; set; } = [];
}

[Table("FinancePettyCashExpenses")]
public sealed class FinancePettyCashExpense : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinancePettyCashFundId { get; set; }
    [Required, MaxLength(80)] public string Category { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string Description { get; set; } = string.Empty;
    [MaxLength(200)] public string? VendorName { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [MaxLength(200)] public string? ReceiptHash { get; set; }
    public DateTime SpentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinancePettyCashFundId))] public FinancePettyCashFund Fund { get; set; } = null!;
}

[Table("FinancePettyCashCounts")]
public sealed class FinancePettyCashCount : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinancePettyCashFundId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal SystemBalance { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal PhysicalBalance { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Discrepancy { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "OPEN";
    [MaxLength(500)] public string? Notes { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public Guid ConfirmedByUserId { get; set; }
    public DateTime CountedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinancePettyCashFundId))] public FinancePettyCashFund Fund { get; set; } = null!;
}
