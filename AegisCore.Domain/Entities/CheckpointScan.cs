using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("CheckpointScans")]
public class CheckpointScan : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid GuardRoundId { get; set; }

    [ForeignKey(nameof(GuardRoundId))]
    public GuardRound GuardRound { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string CheckpointName { get; set; } = string.Empty;

    public DateTime ScanTime { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
