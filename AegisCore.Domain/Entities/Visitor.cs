using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Visitors")]
public class Visitor : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string VisitorName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string CNIC { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Company { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Purpose { get; set; } = string.Empty;

    public Guid? HostEmployeeId { get; set; }

    [ForeignKey(nameof(HostEmployeeId))]
    public Employee? HostEmployee { get; set; }

    public Guid? HostStudentId { get; set; }

    [ForeignKey(nameof(HostStudentId))]
    public Student? HostStudent { get; set; }

    public VisitorType Type { get; set; } = VisitorType.WalkIn;

    public VisitorStatus Status { get; set; } = VisitorStatus.Registered;

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    [MaxLength(50)]
    public string BadgeNumber { get; set; } = string.Empty;

    public bool IsBlacklisted { get; set; }

    [MaxLength(500)]
    public string BlacklistReason { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
