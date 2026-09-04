using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("NoticeBoardItems")]
public class NoticeBoardItem : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(10000)] public string Content { get; set; } = string.Empty;
    public Guid PostedBy { get; set; }
    [MaxLength(50)] public string Visibility { get; set; } = "all";
    public DateTime PostedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("GrievanceTickets")]
public class GrievanceTicket : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [MaxLength(50)] public string TicketNumber { get; set; } = string.Empty;
    public Guid SubmitterId { get; set; }
    [MaxLength(50)] public string Category { get; set; } = "other";
    [MaxLength(200)] public string Subject { get; set; } = string.Empty;
    [MaxLength(10000)] public string Description { get; set; } = string.Empty;
    [MaxLength(20)] public string Status { get; set; } = "open";
    public Guid? AssignedTo { get; set; }
    [MaxLength(20)] public string Priority { get; set; } = "normal";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    [MaxLength(5000)] public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    public ICollection<GrievanceResponse> Responses { get; set; } = new List<GrievanceResponse>();
}

[Table("GrievanceResponses")]
public class GrievanceResponse : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid ResponderId { get; set; }
    [MaxLength(10000)] public string ResponseText { get; set; } = string.Empty;
    [MaxLength(20)] public string? StatusUpdate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(TicketId))] public GrievanceTicket Ticket { get; set; } = null!;
}
