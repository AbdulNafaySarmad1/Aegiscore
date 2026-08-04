using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceAssetAccountingEvents")]
public sealed class FinanceAssetAccountingEvent : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    [Required, MaxLength(20)] public string EventType { get; set; } = string.Empty;
    [Column(TypeName = "numeric(19,4)")] public decimal PreviousCarryingValue { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal NewCarryingValue { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Proceeds { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
