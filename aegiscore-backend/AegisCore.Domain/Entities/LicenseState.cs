using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("LicenseStates")]
public class LicenseState
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string LicenseKey { get; set; } = string.Empty;

    public Guid UniversityId { get; set; }

    [Required, MaxLength(200)]
    public string UniversityName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "active";

    [Required]
    public string LicenseJson { get; set; } = "{}";

    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? LastHeartbeatAt { get; set; }

    [MaxLength(20)]
    public string? LastHeartbeatStatus { get; set; }

    public int FailedHeartbeatCount { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
    public string? LockReason { get; set; }
    public Guid? OverrideBy { get; set; }
    public string? OverrideReason { get; set; }
    public DateTimeOffset? OverrideUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
