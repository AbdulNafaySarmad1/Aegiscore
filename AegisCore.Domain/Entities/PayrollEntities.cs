using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("PayrollRuns")]
public class PayrollRun : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public DateOnly Month { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "draft";
    public int TotalEmployees { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal TotalSalary { get; set; }
    public DateTimeOffset RunDate { get; set; } = DateTimeOffset.UtcNow;
    public Guid? RunBy { get; set; }
    public ICollection<Payslip> Payslips { get; set; } = [];
}

[Table("Payslips")]
public class Payslip : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly Month { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal BasicSalary { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal GrossSalary { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal EobiDeduction { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal IncomeTaxDeduction { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal ProvidentFundDeduction { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal TotalDeductions { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal NetSalary { get; set; }
    public string? PdfFilePath { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public PayrollRun PayrollRun { get; set; } = null!;
}
