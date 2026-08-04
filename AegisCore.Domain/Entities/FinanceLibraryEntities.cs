using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceLibraryAcquisitions")]
public sealed class FinanceLibraryAcquisition : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid? LibraryBookId { get; set; }
    [Required, MaxLength(20)] public string ResourceType { get; set; } = "PHYSICAL";
    [Required, MaxLength(300)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal Cost { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal FairValue { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal AccumulatedAmortization { get; set; }
    public int UsefulLifeMonths { get; set; }
    public DateTime AcquiredAt { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }
    [Required, MaxLength(20)] public string AccountingTreatment { get; set; } = "EXPENSE";
    [Required, MaxLength(20)] public string Status { get; set; } = "ACTIVE";
    public Guid? JournalEntryId { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceLibraryFines")]
public sealed class FinanceLibraryFine : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid? LibraryBookId { get; set; }
    [Required, MaxLength(30)] public string FineType { get; set; } = "LATE_RETURN";
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Required, MaxLength(20)] public string Status { get; set; } = "OPEN";
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid TenantId { get; set; }
}
