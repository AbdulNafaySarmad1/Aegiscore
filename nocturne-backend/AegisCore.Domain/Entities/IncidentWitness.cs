using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("IncidentWitnesses")]
public class IncidentWitness
{
    [Key]
    public Guid Id { get; set; }

    public Guid SecurityIncidentId { get; set; }

    [ForeignKey(nameof(SecurityIncidentId))]
    public SecurityIncident SecurityIncident { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string WitnessName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Statement { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
