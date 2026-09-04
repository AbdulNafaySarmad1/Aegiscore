using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("WorkflowSteps")]
public class WorkflowStep : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; set; }

    public Guid InstanceId { get; set; }

    [Required]
    [MaxLength(100)]
    public string NodeId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string NodeLabel { get; set; } = string.Empty;

    public WorkflowNodeType NodeType { get; set; }

    public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.Pending;

    public int Sequence { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "jsonb")]
    public string InputJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string OutputJson { get; set; } = "{}";

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    [ForeignKey(nameof(InstanceId))]
    public WorkflowInstance Instance { get; set; } = null!;

    public ICollection<ApprovalRecord> ApprovalRecords { get; set; } = new List<ApprovalRecord>();
}
