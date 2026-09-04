using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("FeatureFlags")]
public class FeatureFlag : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public FlagType FlagType { get; set; } = FlagType.Feature;

    public bool IsEnabled { get; set; }

    [MaxLength(100)]
    public string? Module { get; set; }

    [Column(TypeName = "jsonb")]
    public string TenantOverridesJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string RulesJson { get; set; } = "[]";

    public DateTime? ActiveFrom { get; set; }

    public DateTime? ActiveTo { get; set; }

    [MaxLength(200)]
    public string? Tags { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
