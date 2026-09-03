using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("DecisionTables")]
public class DecisionTable : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public RuleDomain Domain { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public string InputColumnsJson { get; set; } = "[]";

    [Required]
    [Column(TypeName = "jsonb")]
    public string OutputColumnsJson { get; set; } = "[]";

    [Required]
    [Column(TypeName = "jsonb")]
    public string RowsJson { get; set; } = "[]";

    public RuleStatus Status { get; set; } = RuleStatus.Draft;

    public int Priority { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
