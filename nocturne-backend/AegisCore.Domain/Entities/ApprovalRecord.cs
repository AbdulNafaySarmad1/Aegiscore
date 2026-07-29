using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ApprovalRecords")]
public class ApprovalRecord
{
    [Key]
    public Guid Id { get; set; }

    public Guid StepId { get; set; }

    public Guid ApproverUserId { get; set; }

    public int Sequence { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public Guid? DelegatedToUserId { get; set; }

    [MaxLength(500)]
    public string? DelegationReason { get; set; }

    [MaxLength(100)]
    public string ApproverRole { get; set; } = string.Empty;

    public DateTime? DecidedAt { get; set; }

    public DateTime TimeoutAt { get; set; }

    public bool IsEscalated { get; set; }

    public Guid? EscalatedToUserId { get; set; }

    [ForeignKey(nameof(StepId))]
    public WorkflowStep Step { get; set; } = null!;
}
