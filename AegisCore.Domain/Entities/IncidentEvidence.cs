using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("IncidentEvidences")]
public class IncidentEvidence : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid SecurityIncidentId { get; set; }

    [ForeignKey(nameof(SecurityIncidentId))]
    public SecurityIncident SecurityIncident { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string EvidenceType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string FileUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
