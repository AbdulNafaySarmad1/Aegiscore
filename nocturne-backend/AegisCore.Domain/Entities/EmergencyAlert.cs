using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("EmergencyAlerts")]
public class EmergencyAlert
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string AlertMessage { get; set; } = string.Empty;

    public EmergencyAlertType Type { get; set; }

    public EmergencyAlertSeverity Severity { get; set; } = EmergencyAlertSeverity.High;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Guid SentByUserId { get; set; }

    public int RecipientCount { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
