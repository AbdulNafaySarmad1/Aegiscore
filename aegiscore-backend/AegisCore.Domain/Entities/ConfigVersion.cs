using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ConfigVersions")]
public class ConfigVersion
{
    [Key]
    public Guid Id { get; set; }

    public Guid ConfigurationEntryId { get; set; }

    public int VersionNumber { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public string ValueJson { get; set; } = "{}";

    [MaxLength(500)]
    public string? ChangeDescription { get; set; }

    public Guid ChangedByUserId { get; set; }

    [MaxLength(200)]
    public string? ChangedByName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ConfigurationEntryId))]
    public ConfigurationEntry ConfigurationEntry { get; set; } = null!;
}
