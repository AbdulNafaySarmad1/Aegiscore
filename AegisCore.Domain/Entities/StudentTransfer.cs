using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("StudentTransfers")]
public class StudentTransfer : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid SourceCampusId { get; set; }

    public Guid DestinationCampusId { get; set; }

    public TransferRequestStatus Status { get; set; } = TransferRequestStatus.Draft;

    [MaxLength(2000)]
    public string? Reason { get; set; }

    [MaxLength(500)]
    public string? SourceApprovedBy { get; set; }

    public DateTime? SourceApprovedAt { get; set; }

    [MaxLength(500)]
    public string? DestApprovedBy { get; set; }

    public DateTime? DestApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student Student { get; set; } = null!;
}
