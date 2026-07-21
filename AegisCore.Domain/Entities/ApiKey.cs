using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ApiKeys")]
public class ApiKey
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string KeyHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string KeyPrefix { get; set; } = string.Empty;

    public ApiKeyTier Tier { get; set; } = ApiKeyTier.Free;

    public ApiKeyStatus Status { get; set; } = ApiKeyStatus.Active;

    public ApiPermission Permission { get; set; } = ApiPermission.ReadOnly;

    [MaxLength(500)]
    public string? AllowedDomains { get; set; }

    [MaxLength(200)]
    public string OwnerName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? OwnerEmail { get; set; }

    public int RateLimitPerMinute { get; set; } = 60;

    public int RateLimitPerDay { get; set; } = 10000;

    public long TotalRequests { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(500)]
    public string? RevocationReason { get; set; }

    public Guid TenantId { get; set; }
}
