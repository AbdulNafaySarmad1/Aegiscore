using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("GuardShifts")]
public class GuardShift
{
    [Key]
    public Guid Id { get; set; }

    public Guid SecurityGuardId { get; set; }

    [ForeignKey(nameof(SecurityGuardId))]
    public SecurityGuard SecurityGuard { get; set; } = null!;

    public DateTime ShiftDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [MaxLength(100)]
    public string Post { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Gate { get; set; } = string.Empty;

    public ShiftStatus Status { get; set; } = ShiftStatus.Scheduled;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
