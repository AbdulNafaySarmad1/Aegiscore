using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("PluginRevenueShares")]
public class PluginRevenueShare : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }
    public Guid ExtensionId { get; set; }
    public Guid TenantId { get; set; }
    public string DeveloperEmail { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal AegisCoreAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal DeveloperAmount { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public Extension? Extension { get; set; }
}
