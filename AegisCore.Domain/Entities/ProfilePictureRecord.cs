using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("ProfilePictureRecords")]
public class ProfilePictureRecord : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid UserId { get; set; }

    [MaxLength(10)] public string ContentType { get; set; } = "image/jpeg";
    public long FileSizeBytes { get; set; }

    // Face validation
    public bool FaceValidated { get; set; }
    public double? FaceConfidence { get; set; }
    public bool EditingDetected { get; set; }
    public double? EditingProbability { get; set; }

    // Storage
    [MaxLength(1000)] public string? FullStorageUrl { get; set; }
    [MaxLength(1000)] public string? ThumbnailStorageUrl { get; set; }
    [MaxLength(500)] public string? StorageKey { get; set; }

    // Audit
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastValidatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(500)] public string? RejectionReason { get; set; }

    // SHA-512 for Document Vault integration
    [MaxLength(128)] public string? Sha512Hash { get; set; }

    public Guid TenantId { get; set; }
}
