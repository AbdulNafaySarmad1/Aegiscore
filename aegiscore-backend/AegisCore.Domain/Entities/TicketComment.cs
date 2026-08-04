using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("TicketComments")]
public class TicketComment
{
    [Key]
    public Guid Id { get; set; }

    public Guid MaintenanceTicketId { get; set; }

    [ForeignKey(nameof(MaintenanceTicketId))]
    public MaintenanceTicket MaintenanceTicket { get; set; } = null!;

    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;

    public Guid CommentedByUserId { get; set; }

    public DateTime CommentedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
