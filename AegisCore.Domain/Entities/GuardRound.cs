using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("GuardRounds")]
public class GuardRound : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid SecurityGuardId { get; set; }

    [ForeignKey(nameof(SecurityGuardId))]
    public SecurityGuard SecurityGuard { get; set; } = null!;

    public DateTime RoundStartTime { get; set; }

    public DateTime? RoundEndTime { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CheckpointScan> CheckpointScans { get; set; } = new List<CheckpointScan>();
}
