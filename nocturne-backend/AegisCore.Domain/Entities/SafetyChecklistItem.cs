using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("SafetyChecklistItems")]
public class SafetyChecklistItem
{
    [Key]
    public Guid Id { get; set; }

    public Guid LabSafetyInspectionId { get; set; }

    [ForeignKey(nameof(LabSafetyInspectionId))]
    public LabSafetyInspection LabSafetyInspection { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string ItemDescription { get; set; } = string.Empty;

    public bool IsCompliant { get; set; }

    [MaxLength(500)]
    public string Remarks { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
