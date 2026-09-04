using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("CisUploads")]
public class CisUpload : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid FacultyId { get; set; }
    [Required] public string CisFilePath { get; set; } = string.Empty;
    public DateTimeOffset UploadDate { get; set; } = DateTimeOffset.UtcNow;
    [Required, MaxLength(20)] public string ProcessingStatus { get; set; } = "pending";
    public string? ExtractedData { get; set; }
    [Required, MaxLength(20)] public string HumanReviewStatus { get; set; } = "pending";
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
