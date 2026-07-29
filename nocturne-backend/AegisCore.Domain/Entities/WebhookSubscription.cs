using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("WebhookSubscriptions")]
public class WebhookSubscription
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string EndpointUrl { get; set; } = string.Empty;

    [MaxLength(200)]
    public string EventType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Secret { get; set; }

    public WebhookStatus Status { get; set; } = WebhookStatus.Active;

    public int MaxRetries { get; set; } = 3;

    public int RetryCount { get; set; }

    public DateTime? LastDeliveredAt { get; set; }

    public DateTime? LastFailedAt { get; set; }

    [MaxLength(2000)]
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
