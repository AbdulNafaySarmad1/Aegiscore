using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("AuditFindings")]
public class AuditFinding
{
    [Key]
    public Guid Id { get; set; }

    public Guid ControlId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public AuditFindingSeverity Severity { get; set; } = AuditFindingSeverity.Observation;

    public AuditFindingStatus Status { get; set; } = AuditFindingStatus.Open;

    public Guid IdentifiedByUserId { get; set; }

    [MaxLength(200)]
    public string? IdentifiedByName { get; set; }

    public DateTime IdentifiedAt { get; set; } = DateTime.UtcNow;

    public DateTime? TargetResolutionDate { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    public bool IsLegalHold { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(ControlId))]
    public ComplianceControl Control { get; set; } = null!;

    public ICollection<CorrectiveActionPlan> ActionPlans { get; set; } = new List<CorrectiveActionPlan>();
}
