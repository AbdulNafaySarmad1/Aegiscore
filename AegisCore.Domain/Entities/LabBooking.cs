using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("LabBookings")]
public class LabBooking : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid ResearchLabId { get; set; }

    [ForeignKey(nameof(ResearchLabId))]
    public ResearchLab ResearchLab { get; set; } = null!;

    public Guid BookedByUserId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    public int ExpectedAttendees { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public Guid? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(500)]
    public string RejectionReason { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
