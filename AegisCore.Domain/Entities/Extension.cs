using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("Extensions")]
public class Extension : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = "1.0.0";

    public ExtensionCategory Category { get; set; }

    public ExtensionStatus Status { get; set; } = ExtensionStatus.Draft;

    public ExtensionPricingModel PricingModel { get; set; } = ExtensionPricingModel.Free;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string? IconUrl { get; set; }

    [Column(TypeName = "jsonb")]
    public string ScreenshotsJson { get; set; } = "[]";

    [Required]
    [Column(TypeName = "jsonb")]
    public string ManifestJson { get; set; } = "{}";

    [Required]
    [MaxLength(200)]
    public string DeveloperName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? DeveloperEmail { get; set; }

    [MaxLength(500)]
    public string? DeveloperWebsite { get; set; }

    [MaxLength(100)]
    public string? PackageChecksum { get; set; }

    [MaxLength(500)]
    public string? PackageUrl { get; set; }

    [Column(TypeName = "jsonb")]
    public string DependenciesJson { get; set; } = "[]";

    [Column(TypeName = "jsonb")]
    public string RequiredPermissionsJson { get; set; } = "[]";

    public long DownloadCount { get; set; }

    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; set; }

    public Guid TenantId { get; set; }

    public ICollection<ExtensionInstallation> Installations { get; set; } = new List<ExtensionInstallation>();
    public ICollection<ExtensionReview> Reviews { get; set; } = new List<ExtensionReview>();
}
