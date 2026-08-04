using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceUtilityBills")]
public sealed class FinanceUtilityBill : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(30)] public string UtilityType { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Provider { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string AccountNumber { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal AmountDue { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal UnitsConsumed { get; set; }
    public DateTime BillingPeriodStart { get; set; }
    public DateTime BillingPeriodEnd { get; set; }
    public DateTime DueDate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "BILL_RECEIVED";
    public Guid? JournalEntryId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FinanceUtilityAllocation> Allocations { get; set; } = [];
}

[Table("FinanceUtilityAllocations")]
public sealed class FinanceUtilityAllocation : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinanceUtilityBillId { get; set; }
    public Guid CostCenterId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinanceUtilityBillId))] public FinanceUtilityBill Bill { get; set; } = null!;
}
