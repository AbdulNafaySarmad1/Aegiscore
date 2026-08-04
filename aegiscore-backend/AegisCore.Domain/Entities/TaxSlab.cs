using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("TaxSlabs")]
public class TaxSlab
{
    [Key]
    public Guid Id { get; set; }

    public Guid CountryPackId { get; set; }

    public TaxType TaxType { get; set; } = TaxType.IncomeTax;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxAmount { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal Rate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FixedDeduction { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(CountryPackId))]
    public CountryPack CountryPack { get; set; } = null!;
}
