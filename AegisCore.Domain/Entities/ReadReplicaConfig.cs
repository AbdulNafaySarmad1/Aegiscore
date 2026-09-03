using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("ReadReplicaConfigs")]
public class ReadReplicaConfig : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string ConnectionString { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Priority { get; set; }

    public ReplicaStatus Status { get; set; } = ReplicaStatus.Online;

    public int MaxLagSeconds { get; set; } = 60;

    public int? CurrentLagSeconds { get; set; }

    public int Weight { get; set; } = 1;

    public bool IsEnabled { get; set; } = true;

    public DateTime? LastHealthCheckAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
