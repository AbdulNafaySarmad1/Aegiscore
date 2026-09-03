using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("LostFoundItems")]
public class LostFoundItem : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public LostFoundType Type { get; set; }

    public DateTime FoundDate { get; set; }

    [MaxLength(100)]
    public string FoundLocation { get; set; } = string.Empty;

    [MaxLength(500)]
    public string PhotoUrl { get; set; } = string.Empty;

    public LostFoundStatus Status { get; set; } = LostFoundStatus.Unclaimed;

    public Guid? ClaimedByUserId { get; set; }

    public DateTime? ClaimedAt { get; set; }

    public Guid? HandedOverByUserId { get; set; }

    public Guid TenantId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
