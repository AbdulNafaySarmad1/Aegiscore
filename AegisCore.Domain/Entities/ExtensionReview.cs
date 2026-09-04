using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ExtensionReviews")]
public class ExtensionReview : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid ExtensionId { get; set; }

    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    [MaxLength(200)]
    public string ReviewedByName { get; set; } = string.Empty;

    public Guid ReviewedByUserId { get; set; }

    [MaxLength(2000)]
    public string? DeveloperResponse { get; set; }

    public DateTime? DeveloperResponseAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(ExtensionId))]
    public Extension Extension { get; set; } = null!;
}
