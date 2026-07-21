using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("CalibrationRecords")]
public class CalibrationRecord
{
    [Key]
    public Guid Id { get; set; }

    public Guid LabEquipmentId { get; set; }

    [ForeignKey(nameof(LabEquipmentId))]
    public LabEquipment LabEquipment { get; set; } = null!;

    public DateTime CalibrationDate { get; set; }

    public DateTime NextCalibrationDue { get; set; }

    [MaxLength(200)]
    public string CalibratedBy { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Results { get; set; } = string.Empty;

    public CalibrationStatus Status { get; set; } = CalibrationStatus.Completed;

    [MaxLength(500)]
    public string CertificateUrl { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
