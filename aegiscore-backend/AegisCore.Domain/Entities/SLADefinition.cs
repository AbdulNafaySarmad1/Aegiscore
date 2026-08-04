using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("SLADefinitions")]
public class SLADefinition
{
    [Key]
    public Guid Id { get; set; }

    public MaintenanceCategory Category { get; set; }

    public TicketPriority Priority { get; set; }

    public int ResponseTimeHours { get; set; }

    public int ResolutionTimeHours { get; set; }

    public int EscalationTimeHours { get; set; }

    public EscalationLevel EscalationLevel { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
