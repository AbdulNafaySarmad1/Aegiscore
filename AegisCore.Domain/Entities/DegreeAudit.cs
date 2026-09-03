using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("DegreeAudits")]
public class DegreeAudit : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid ProgramId { get; set; }

    public Guid RequirementId { get; set; }

    public AuditStatus Status { get; set; } = AuditStatus.NotStarted;

    public decimal? EarnedValue { get; set; }

    public decimal? RequiredValue { get; set; }

    public int? CompletedCount { get; set; }

    public int? RequiredCount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime LastEvaluatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student Student { get; set; } = null!;

    [ForeignKey(nameof(ProgramId))]
    public DegreeProgram Program { get; set; } = null!;

    [ForeignKey(nameof(RequirementId))]
    public DegreeRequirement Requirement { get; set; } = null!;
}
