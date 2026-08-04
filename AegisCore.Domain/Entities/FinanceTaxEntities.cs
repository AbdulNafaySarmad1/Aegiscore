using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceTaxRules")]
public sealed class FinanceTaxRule : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(30)] public string TaxType { get; set; } = "WHT_VENDOR";
    [Required, MaxLength(80)] public string Jurisdiction { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string VendorCategory { get; set; } = "GENERAL";
    [Column(TypeName = "numeric(9,4)")] public decimal FilerRatePercent { get; set; }
    [Column(TypeName = "numeric(9,4)")] public decimal NonFilerMultiplier { get; set; } = 2m;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
}

[Table("FinanceWithholdingRecords")]
public sealed class FinanceWithholdingRecord : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? TaxRuleId { get; set; }
    [Required, MaxLength(30)] public string TaxType { get; set; } = "WHT_VENDOR";
    [Required, MaxLength(20)] public string FilingPeriod { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string PaymentReference { get; set; } = string.Empty;
    [Column(TypeName = "numeric(19,4)")] public decimal GrossAmount { get; set; }
    [Column(TypeName = "numeric(9,4)")] public decimal AppliedRatePercent { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal WithheldAmount { get; set; }
    [Required, MaxLength(20)] public string FilerStatus { get; set; } = "FILER";
    [Required, MaxLength(20)] public string Status { get; set; } = "POSTED";
    public Guid JournalEntryId { get; set; }
    public DateTime WithheldAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
