using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("ConfigurationEntries")]
public class ConfigurationEntry : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    public ConfigurationDomain Domain { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public string ValueJson { get; set; } = "{}";

    [MaxLength(100)]
    public string? SchemaUrl { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public ConfigEntryStatus Status { get; set; } = ConfigEntryStatus.Active;

    public ConfigEnvironment Environment { get; set; } = ConfigEnvironment.Production;

    public int CurrentVersion { get; set; } = 1;

    public Guid? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    public ICollection<ConfigVersion> Versions { get; set; } = new List<ConfigVersion>();
}
