using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("RuleDefinitions")]
public class RuleDefinition
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public RuleDomain Domain { get; set; }

    public RuleStatus Status { get; set; } = RuleStatus.Draft;

    public RuleMatchStrategy MatchStrategy { get; set; } = RuleMatchStrategy.FirstMatch;

    public int Priority { get; set; }

    public int CurrentVersion { get; set; } = 1;

    [Required]
    [Column(TypeName = "jsonb")]
    public string ConditionsJson { get; set; } = "[]";

    [Required]
    [Column(TypeName = "jsonb")]
    public string ActionsJson { get; set; } = "[]";

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(200)]
    public string? Tags { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    public ICollection<RuleVersion> Versions { get; set; } = new List<RuleVersion>();
}
