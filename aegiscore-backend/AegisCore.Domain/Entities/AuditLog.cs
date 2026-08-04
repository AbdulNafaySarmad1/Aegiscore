using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

/// <summary>
/// Represents an audit log entry for critical actions.
/// </summary>
[Table("AuditLogs")]
public class AuditLog : ITenantScoped
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// User identifier who performed the action.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The action performed (CREATE, UPDATE, DELETE, LOGIN, etc.).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The type of resource affected.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// The identifier of the resource affected.
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// Additional details about the action.
    /// </summary>
    [MaxLength(2000)]
    public string? Details { get; set; }

    /// <summary>
    /// Whether the action was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Timestamp of the action.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address of the user.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }
}
