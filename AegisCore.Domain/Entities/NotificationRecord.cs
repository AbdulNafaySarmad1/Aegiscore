using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("NotificationRecords")]
public class NotificationRecord : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; set; }

    public Guid InstanceId { get; set; }

    public NotificationChannel Channel { get; set; }

    [Required]
    [MaxLength(500)]
    public string Recipient { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string Body { get; set; } = string.Empty;

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public DateTime? SentAt { get; set; }

    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(InstanceId))]
    public WorkflowInstance Instance { get; set; } = null!;
}
