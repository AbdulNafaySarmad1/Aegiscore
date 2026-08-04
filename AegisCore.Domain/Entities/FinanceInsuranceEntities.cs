using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceInsurancePolicies")]
public sealed class FinanceInsurancePolicy : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(200)] public string Insurer { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string PolicyNumber { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string PolicyType { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal CoverageAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Premium { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime RenewalDate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "ACTIVE";
    public DateTime? AmortizedThrough { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FinanceInsuranceClaim> Claims { get; set; } = [];
}

[Table("FinanceInsuranceClaims")]
public sealed class FinanceInsuranceClaim : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinanceInsurancePolicyId { get; set; }
    [Required, MaxLength(80)] public string ClaimNumber { get; set; } = string.Empty;
    [Required, MaxLength(1000)] public string Description { get; set; } = string.Empty;
    [Column(TypeName = "numeric(19,4)")] public decimal ClaimedAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal SettledAmount { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "FILED";
    public DateTime FiledAt { get; set; } = DateTime.UtcNow;
    public DateTime? SettledAt { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinanceInsurancePolicyId))] public FinanceInsurancePolicy Policy { get; set; } = null!;
}
