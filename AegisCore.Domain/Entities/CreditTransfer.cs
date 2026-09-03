using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("CreditTransfers")]
public class CreditTransfer : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    [MaxLength(200)]
    public string ExternalCourseName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ExternalCourseCode { get; set; }

    [MaxLength(200)]
    public string ExternalInstitution { get; set; } = string.Empty;

    public int CreditHours { get; set; }

    public Guid? InternalCourseId { get; set; }

    [MaxLength(50)]
    public string? InternalCourseCode { get; set; }

    public bool IsApproved { get; set; }

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(200)]
    public string? ApprovedBy { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student Student { get; set; } = null!;
}
