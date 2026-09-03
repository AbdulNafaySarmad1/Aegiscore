using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("Currencies")]
public class Currency : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Symbol { get; set; } = "$";

    public int DecimalPlaces { get; set; } = 2;

    [MaxLength(5)]
    public string DecimalSeparator { get; set; } = ".";

    [MaxLength(5)]
    public string ThousandsSeparator { get; set; } = ",";

    [Column(TypeName = "decimal(18,6)")]
    public decimal ExchangeRateToBase { get; set; } = 1.0m;

    public DateTime ExchangeRateUpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsBaseCurrency { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
