using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("EquipmentBookings")]
public class EquipmentBooking : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid LabEquipmentId { get; set; }

    [ForeignKey(nameof(LabEquipmentId))]
    public LabEquipment LabEquipment { get; set; } = null!;

    public Guid BookedByUserId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [MaxLength(500)]
    public string Purpose { get; set; } = string.Empty;

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public Guid? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
