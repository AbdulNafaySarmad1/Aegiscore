using AegisCore.Domain.Common;
namespace AegisCore.Infrastructure;

/// <summary>
/// Represents a scaling event in the system.
/// </summary>
public class ScalingEvent : ITenantScoped
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of the scaling event.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Optional replica name.
    /// </summary>
    public string? ReplicaName { get; set; }

    /// <summary>
    /// Total number of replicas after the event.
    /// </summary>
    public int TotalReplicas { get; set; }

    /// <summary>
    /// Timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }
}
