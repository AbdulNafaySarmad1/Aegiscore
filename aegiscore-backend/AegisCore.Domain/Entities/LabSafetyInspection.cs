using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("LabSafetyInspections")]
public class LabSafetyInspection
{
    [Key]
    public Guid Id { get; set; }

    public Guid ResearchLabId { get; set; }

    [ForeignKey(nameof(ResearchLabId))]
    public ResearchLab ResearchLab { get; set; } = null!;

    public DateTime InspectionDate { get; set; }

    public Guid InspectedByUserId { get; set; }

    public InspectionStatus Status { get; set; } = InspectionStatus.Scheduled;

    [MaxLength(1000)]
    public string Findings { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string CorrectiveActions { get; set; } = string.Empty;

    public DateTime? CorrectiveActionsDue { get; set; }

    public bool IsCompliant { get; set; }

    public Guid? SignedOffByUserId { get; set; }

    public DateTime? SignedOffAt { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<SafetyChecklistItem> ChecklistItems { get; set; } = new List<SafetyChecklistItem>();
}
