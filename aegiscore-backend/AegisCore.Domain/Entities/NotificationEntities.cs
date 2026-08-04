using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("NotificationTemplates")]
public class NotificationTemplate
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public NotificationTemplateChannel Channel { get; set; } = NotificationTemplateChannel.Email;

    [Required]
    [MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(10000)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Module { get; set; }

    [MaxLength(50)]
    public string LanguageCode { get; set; } = "en";

    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("NotificationPreferences")]
public class UserNotificationPreference
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string EnabledChannelsJson { get; set; } = "[\"Email\",\"InApp\"]";

    [MaxLength(50)]
    public string? DoNotDisturbFrom { get; set; }

    [MaxLength(50)]
    public string? DoNotDisturbTo { get; set; }

    public Guid TenantId { get; set; }
}

[Table("NotificationDeliveries")]
public class NotificationDelivery
{
    [Key]
    public Guid Id { get; set; }

    public NotificationTemplateChannel Channel { get; set; }

    [MaxLength(500)]
    public string Recipient { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Module { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Queued;

    public int RetryCount { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? OpenedAt { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
