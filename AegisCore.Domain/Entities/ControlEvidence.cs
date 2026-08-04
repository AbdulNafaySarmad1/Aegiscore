using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("ControlEvidences")]
public class ControlEvidence : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid ControlId { get; set; }

    [MaxLength(500)]
    public string FileUrl { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EvidenceType { get; set; }

    public Guid UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(ControlId))]
    public ComplianceControl Control { get; set; } = null!;
}
