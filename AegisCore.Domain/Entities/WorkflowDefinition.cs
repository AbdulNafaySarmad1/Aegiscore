using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("WorkflowDefinitions")]
public class WorkflowDefinition : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public WorkflowDefinitionStatus Status { get; set; } = WorkflowDefinitionStatus.Draft;

    [Column(TypeName = "jsonb")]
    public string NodesJson { get; set; } = "[]";

    [Column(TypeName = "jsonb")]
    public string TransitionsJson { get; set; } = "[]";

    public WorkflowTriggerType TriggerType { get; set; }

    [MaxLength(200)]
    public string? TriggerConfig { get; set; }

    public bool IsTemplate { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    public ICollection<WorkflowInstance> Instances { get; set; } = new List<WorkflowInstance>();
}
