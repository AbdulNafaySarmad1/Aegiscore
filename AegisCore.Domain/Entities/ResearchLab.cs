using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ResearchLabs")]
public class ResearchLab : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string LabName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string LabCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string RoomNumber { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    [ForeignKey(nameof(DepartmentId))]
    public Department Department { get; set; } = null!;

    public Guid? LabSupervisorId { get; set; }

    [ForeignKey(nameof(LabSupervisorId))]
    public Employee? LabSupervisor { get; set; }

    public int Capacity { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public LabStatus Status { get; set; } = LabStatus.Active;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<LabEquipment> Equipment { get; set; } = new List<LabEquipment>();
    public ICollection<LabBooking> Bookings { get; set; } = new List<LabBooking>();
    public ICollection<LabConsumable> Consumables { get; set; } = new List<LabConsumable>();
    public ICollection<LabSafetyInspection> SafetyInspections { get; set; } = new List<LabSafetyInspection>();
}
