using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceForeignCurrencyTransactions")]
public sealed class FinanceForeignCurrencyTransaction : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(20)] public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ForeignAmount { get; set; }
    [Required, MaxLength(3)] public string ForeignCurrency { get; set; } = string.Empty;
    [Column(TypeName = "numeric(19,4)")] public decimal FunctionalAmount { get; set; }
    [Required, MaxLength(3)] public string FunctionalCurrency { get; set; } = string.Empty;
    [Column(TypeName = "numeric(24,12)")] public decimal RateUsed { get; set; }
    [Required, MaxLength(20)] public string RateSource { get; set; } = string.Empty;
    public DateTime RateDate { get; set; }
    public bool IsSettled { get; set; }
    public DateTime? SettledAt { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal RealizedFxGainLoss { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
