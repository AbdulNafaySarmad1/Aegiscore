using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("SecurityIncidents")]
public class SecurityIncident : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string IncidentNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public IncidentType Type { get; set; }

    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;

    public IncidentStatus Status { get; set; } = IncidentStatus.Reported;

    public DateTime IncidentDateTime { get; set; }

    public Guid? LocationBuildingId { get; set; }

    [ForeignKey(nameof(LocationBuildingId))]
    public Building? LocationBuilding { get; set; }

    public Guid? LocationRoomId { get; set; }

    [ForeignKey(nameof(LocationRoomId))]
    public Room? LocationRoom { get; set; }

    public Guid ReportedByUserId { get; set; }

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedToEmployeeId { get; set; }

    [ForeignKey(nameof(AssignedToEmployeeId))]
    public Employee? AssignedTo { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(1000)]
    public string ResolutionNotes { get; set; } = string.Empty;

    public bool IsImmutable { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<IncidentWitness> Witnesses { get; set; } = new List<IncidentWitness>();
    public ICollection<IncidentEvidence> Evidences { get; set; } = new List<IncidentEvidence>();
}
