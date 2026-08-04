using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinancePayrollPostings")]
public sealed class FinancePayrollPosting : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(7)] public string Period { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Status { get; set; } = "IMPORTED";
    [Column(TypeName = "numeric(19,4)")] public decimal GrossSalary { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal NetSalary { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal IncomeTax { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Eobi { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal SocialSecurity { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Gratuity { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid ImportedByUserId { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    public ICollection<FinancePayrollPostingLine> Lines { get; set; } = [];
}

[Table("FinancePayrollPostingLines")]
public sealed class FinancePayrollPostingLine : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FinancePayrollPostingId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CostCenterId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal GrossSalary { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal IncomeTax { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Eobi { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal SocialSecurity { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Gratuity { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal NetSalary { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FinancePayrollPostingId))] public FinancePayrollPosting Posting { get; set; } = null!;
}
