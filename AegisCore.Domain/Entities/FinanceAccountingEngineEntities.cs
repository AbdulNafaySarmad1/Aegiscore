using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceAccountingStandardPolicies")]
public sealed class FinanceAccountingStandardPolicy : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(20)] public string StandardCode { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string PolicyCode { get; set; } = string.Empty;
    [Required, Column(TypeName = "jsonb")] public string ValueJson { get; set; } = "{}";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceReportingEntities")]
public sealed class FinanceReportingEntity : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(30)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string FunctionalCurrency { get; set; } = "PKR";
    public Guid? ParentEntityId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
}

[Table("FinanceRestatementRules")]
public sealed class FinanceRestatementRule : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(20)] public string SourceStandard { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string TargetStandard { get; set; } = string.Empty;
    public Guid SourceAccountId { get; set; }
    public Guid TargetAccountId { get; set; }
    [Column(TypeName = "numeric(19,8)")] public decimal Factor { get; set; } = 1m;
    [Required, MaxLength(500)] public string Rationale { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
}

[Table("FinanceRestatementRuns")]
public sealed class FinanceRestatementRun : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(20)] public string SourceStandard { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string TargetStandard { get; set; } = string.Empty;
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "COMPLETED";
    public Guid TenantId { get; set; }
    public ICollection<FinanceRestatementMemoLine> Lines { get; set; } = [];
}

[Table("FinanceRestatementMemoLines")]
public sealed class FinanceRestatementMemoLine : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid RestatementRunId { get; set; }
    public Guid RestatementRuleId { get; set; }
    public Guid ChartOfAccountId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Debit { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Credit { get; set; }
    [Required, MaxLength(500)] public string Description { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(RestatementRunId))] public FinanceRestatementRun Run { get; set; } = null!;
}

[Table("FinanceConsolidationGroups")]
public sealed class FinanceConsolidationGroup : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(160)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string PresentationCurrency { get; set; } = "PKR";
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
    public ICollection<FinanceConsolidationMember> Members { get; set; } = [];
}

[Table("FinanceConsolidationMembers")]
public sealed class FinanceConsolidationMember : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ConsolidationGroupId { get; set; }
    public Guid ReportingEntityId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(ConsolidationGroupId))] public FinanceConsolidationGroup Group { get; set; } = null!;
}

[Table("FinanceEliminationRules")]
public sealed class FinanceEliminationRule : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ConsolidationGroupId { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid CounterpartAccountId { get; set; }
    [Required, MaxLength(500)] public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
}

[Table("FinanceConsolidationRuns")]
public sealed class FinanceConsolidationRun : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ConsolidationGroupId { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public Guid TenantId { get; set; }
    public ICollection<FinanceConsolidationMemoLine> Lines { get; set; } = [];
}

[Table("FinanceConsolidationMemoLines")]
public sealed class FinanceConsolidationMemoLine : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ConsolidationRunId { get; set; }
    public Guid ChartOfAccountId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal LedgerBalance { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal EliminationAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ConsolidatedBalance { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(ConsolidationRunId))] public FinanceConsolidationRun Run { get; set; } = null!;
}
