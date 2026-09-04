using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("GateEntries")]
public class GateEntry : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid? VehiclePassId { get; set; }

    [ForeignKey(nameof(VehiclePassId))]
    public VehiclePass? VehiclePass { get; set; }

    public Guid? VisitorId { get; set; }

    [ForeignKey(nameof(VisitorId))]
    public Visitor? Visitor { get; set; }

    [Required]
    [MaxLength(100)]
    public string GateName { get; set; } = string.Empty;

    public DateTime EntryTime { get; set; }

    public DateTime? ExitTime { get; set; }

    public Guid? GuardEmployeeId { get; set; }

    [ForeignKey(nameof(GuardEmployeeId))]
    public Employee? Guard { get; set; }

    public EntryType Type { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
