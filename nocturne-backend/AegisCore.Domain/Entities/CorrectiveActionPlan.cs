using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("CorrectiveActionPlans")]
public class CorrectiveActionPlan
{
    [Key]
    public Guid Id { get; set; }

    public Guid FindingId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public Guid AssignedToUserId { get; set; }

    [MaxLength(200)]
    public string? AssignedToName { get; set; }

    public CAPStatus Status { get; set; } = CAPStatus.Planned;

    public DateTime DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    [MaxLength(2000)]
    public string? CompletionNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(FindingId))]
    public AuditFinding Finding { get; set; } = null!;
}
