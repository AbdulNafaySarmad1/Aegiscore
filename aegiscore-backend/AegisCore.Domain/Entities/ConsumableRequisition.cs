using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ConsumableRequisitions")]
public class ConsumableRequisition
{
    [Key]
    public Guid Id { get; set; }

    public Guid LabConsumableId { get; set; }

    [ForeignKey(nameof(LabConsumableId))]
    public LabConsumable LabConsumable { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }

    public decimal Quantity { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public RequisitionStatus Status { get; set; } = RequisitionStatus.Pending;

    public Guid? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? IssuedByUserId { get; set; }

    public DateTime? IssuedAt { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public Guid TenantId { get; set; }
}
