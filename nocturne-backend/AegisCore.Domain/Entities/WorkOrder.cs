using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("WorkOrders")]
public class WorkOrder
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string WorkOrderNumber { get; set; } = string.Empty;

    public Guid MaintenanceTicketId { get; set; }

    [ForeignKey(nameof(MaintenanceTicketId))]
    public MaintenanceTicket MaintenanceTicket { get; set; } = null!;

    public Guid? AssignedToEmployeeId { get; set; }

    [ForeignKey(nameof(AssignedToEmployeeId))]
    public Employee? AssignedTo { get; set; }

    public Guid? AssignedToContractorId { get; set; }

    [ForeignKey(nameof(AssignedToContractorId))]
    public MaintenanceContractor? AssignedToContractor { get; set; }

    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Assigned;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? SignedOffAt { get; set; }

    public Guid? SignedOffByUserId { get; set; }

    [MaxLength(1000)]
    public string ResolutionNotes { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal LaborCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaterialCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    public decimal HoursWorked { get; set; }

    public bool IsPreventiveMaintenance { get; set; }

    public Guid? PMScheduleId { get; set; }

    [ForeignKey(nameof(PMScheduleId))]
    public PreventiveMaintenanceSchedule? PMSchedule { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<WorkOrderMaterial> Materials { get; set; } = new List<WorkOrderMaterial>();
    public ICollection<WorkOrderTimeEntry> TimeEntries { get; set; } = new List<WorkOrderTimeEntry>();
}
