using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("AccreditationFrameworks")]
public class AccreditationFramework : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(50)] public string FrameworkCode { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string FrameworkName { get; set; } = string.Empty;
    [MaxLength(50)] public string? AccreditationBody { get; set; }
    public DateTimeOffset? LastAuditDate { get; set; }
    public DateTimeOffset? NextAuditDate { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal CurrentScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    public ICollection<ReadinessCriterion> Criteria { get; set; } = [];
}

[Table("ReadinessCriteria")]
public class ReadinessCriterion : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FrameworkId { get; set; }
    [Required, MaxLength(20)] public string CriterionCode { get; set; } = string.Empty;
    [MaxLength(100)] public string? SectionName { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [Column(TypeName = "decimal(3,2)")] public decimal Weight { get; set; } = 1.0m;
    [Column(TypeName = "decimal(3,2)")] public decimal Score { get; set; }
    [Required, MaxLength(20)] public string ComplianceStatus { get; set; } = "not_met";
    [MaxLength(200)] public string? EvidenceSource { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FrameworkId))] public AccreditationFramework Framework { get; set; } = null!;
}

[Table("CriterionEvidences")]
public class CriterionEvidence : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid CriterionId { get; set; }
    [Required, MaxLength(300)] public string EvidenceTitle { get; set; } = string.Empty;
    [MaxLength(500)] public string? EvidenceUrl { get; set; }
    [MaxLength(2000)] public string? EvidenceDescription { get; set; }
    public DateTimeOffset EvidenceDate { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    public bool IsStale { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("AccreditationScoreSnapshots")]
public class AccreditationScoreSnapshot : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FrameworkId { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal Score { get; set; }
    [MaxLength(20)] public string Interpretation { get; set; } = "needs_improvement";
    public DateTimeOffset SnapshotDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("AccreditationAuditFindings")]
public class AccreditationAuditFinding : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid FrameworkId { get; set; }
    public Guid? CriterionId { get; set; }
    [MaxLength(20)] public string? FindingType { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    [MaxLength(2000)] public string? CorrectiveAction { get; set; }
    [Required, MaxLength(20)] public string CorrectiveStatus { get; set; } = "not_started";
    public DateTimeOffset? DeadlineAt { get; set; }
    public bool IsOverdue { get; set; }
    [MaxLength(100)] public string? ResponsibleOwner { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("GapAnalysisItems")]
public class GapAnalysisItem : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid CriterionId { get; set; }
    [MaxLength(500)] public string? GapDescription { get; set; }
    [MaxLength(500)] public string? EvidenceNeeded { get; set; }
    [MaxLength(100)] public string? ResponsibleOwner { get; set; }
    [MaxLength(20)] public string EstimatedEffort { get; set; } = "medium_term";
    [Column(TypeName = "decimal(4,2)")] public decimal ImpactOnScore { get; set; }
    public int PriorityRank { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
