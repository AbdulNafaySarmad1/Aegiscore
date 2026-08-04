using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("TicketEscalations")]
public class TicketEscalation
{
    [Key]
    public Guid Id { get; set; }

    public Guid MaintenanceTicketId { get; set; }

    [ForeignKey(nameof(MaintenanceTicketId))]
    public MaintenanceTicket MaintenanceTicket { get; set; } = null!;

    public EscalationLevel EscalationLevel { get; set; }

    public DateTime EscalatedAt { get; set; } = DateTime.UtcNow;

    public Guid EscalatedToUserId { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public bool IsResolved { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public Guid TenantId { get; set; }
}
