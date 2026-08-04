using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("LabEquipments")]
public class LabEquipment
{
    [Key]
    public Guid Id { get; set; }

    public Guid ResearchLabId { get; set; }

    [ForeignKey(nameof(ResearchLabId))]
    public ResearchLab ResearchLab { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string EquipmentName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string AssetTag { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [MaxLength(50)]
    public string SerialNumber { get; set; } = string.Empty;

    public DateTime? PurchaseDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchaseCost { get; set; }

    public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;

    [MaxLength(500)]
    public string Specifications { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<EquipmentBooking> Bookings { get; set; } = new List<EquipmentBooking>();
    public ICollection<EquipmentUsageLog> UsageLogs { get; set; } = new List<EquipmentUsageLog>();
    public ICollection<CalibrationRecord> CalibrationRecords { get; set; } = new List<CalibrationRecord>();
    public ICollection<EquipmentMaintenance> MaintenanceRequests { get; set; } = new List<EquipmentMaintenance>();
}
