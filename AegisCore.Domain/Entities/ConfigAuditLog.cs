using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ConfigAuditLogs")]
public class ConfigAuditLog : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid ConfigurationEntryId { get; set; }

    [MaxLength(200)]
    public string ConfigKey { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? PreviousValueJson { get; set; }

    [Column(TypeName = "jsonb")]
    public string? NewValueJson { get; set; }

    public int? FromVersion { get; set; }

    public int? ToVersion { get; set; }

    public Guid ChangedByUserId { get; set; }

    [MaxLength(200)]
    public string? ChangedByName { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(ConfigurationEntryId))]
    public ConfigurationEntry ConfigurationEntry { get; set; } = null!;
}
