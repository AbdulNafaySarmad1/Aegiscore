using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceBankAccounts")]
public sealed class FinanceBankAccount : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(34)] public string AccountReference { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    public Guid ChartOfAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
    public ICollection<FinanceBankFeedTransaction> FeedTransactions { get; set; } = [];
}

[Table("FinanceBankFeedTransactions")]
public sealed class FinanceBankFeedTransaction : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinanceBankAccountId { get; set; }
    [Required, MaxLength(160)] public string ExternalId { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [Required, MaxLength(1000)] public string Narration { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Status { get; set; } = "UNMATCHED";
    public Guid? MatchedJournalEntryId { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinanceBankAccountId))] public FinanceBankAccount BankAccount { get; set; } = null!;
}
