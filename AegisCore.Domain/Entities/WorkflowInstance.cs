using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("WorkflowInstances")]
public class WorkflowInstance : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid DefinitionId { get; set; }

    public int DefinitionVersion { get; set; }

    [MaxLength(200)]
    public string InstanceName { get; set; } = string.Empty;

    public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.Running;

    [MaxLength(100)]
    public string CurrentNodeId { get; set; } = string.Empty;

    public Guid? TriggeredByUserId { get; set; }

    public WorkflowTriggerType TriggerType { get; set; }

    [Column(TypeName = "jsonb")]
    public string ContextJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string VariablesJson { get; set; } = "{}";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(DefinitionId))]
    public WorkflowDefinition Definition { get; set; } = null!;

    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public ICollection<NotificationRecord> Notifications { get; set; } = new List<NotificationRecord>();
}
