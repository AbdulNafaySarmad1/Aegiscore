using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("RuleVersions")]
public class RuleVersion
{
    [Key]
    public Guid Id { get; set; }

    public Guid RuleId { get; set; }

    public int VersionNumber { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public string ConditionsJson { get; set; } = "[]";

    [Required]
    [Column(TypeName = "jsonb")]
    public string ActionsJson { get; set; } = "[]";

    [MaxLength(500)]
    public string? ChangeDescription { get; set; }

    public Guid ChangedByUserId { get; set; }

    [MaxLength(200)]
    public string? ChangedByName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RuleId))]
    public RuleDefinition Rule { get; set; } = null!;
}
