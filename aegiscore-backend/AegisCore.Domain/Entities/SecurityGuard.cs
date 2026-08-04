using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("SecurityGuards")]
public class SecurityGuard
{
    [Key]
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee Employee { get; set; } = null!;

    [MaxLength(50)]
    public string BadgeNumber { get; set; } = string.Empty;

    public bool IsArmed { get; set; }

    public DateTime? TrainingExpiryDate { get; set; }

    public GuardStatus Status { get; set; } = GuardStatus.Active;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<GuardShift> Shifts { get; set; } = new List<GuardShift>();
    public ICollection<GuardRound> Rounds { get; set; } = new List<GuardRound>();
}
