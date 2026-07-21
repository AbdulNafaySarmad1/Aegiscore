using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("SamlConfigurations")]
public class SamlConfiguration
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string IdpSsoUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? IdpSloUrl { get; set; }

    [Required]
    [Column(TypeName = "text")]
    public string IdpCertificate { get; set; } = string.Empty;

    [MaxLength(500)]
    public string SpEntityId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string SpAcsUrl { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string AttributeMappingJson { get; set; } = "{}";

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
