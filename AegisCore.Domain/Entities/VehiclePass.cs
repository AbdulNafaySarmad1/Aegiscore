using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("VehiclePasses")]
public class VehiclePass : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string VehicleNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string VehicleModel { get; set; } = string.Empty;

    [MaxLength(50)]
    public string VehicleColor { get; set; } = string.Empty;

    public VehiclePassType PassType { get; set; }

    public Guid? OwnerEmployeeId { get; set; }

    [ForeignKey(nameof(OwnerEmployeeId))]
    public Employee? OwnerEmployee { get; set; }

    public Guid? OwnerStudentId { get; set; }

    [ForeignKey(nameof(OwnerStudentId))]
    public Student? OwnerStudent { get; set; }

    public Guid? VisitorId { get; set; }

    [ForeignKey(nameof(VisitorId))]
    public Visitor? Visitor { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public VehiclePassStatus Status { get; set; } = VehiclePassStatus.Active;

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<GateEntry> GateEntries { get; set; } = new List<GateEntry>();
}
