using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

/// <summary>
/// Represents the association between a user and a role.
/// </summary>
[Table("UserRoles")]
public class UserRole : ITenantScoped
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// User identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Role identifier.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Identifier of the user who assigned this role.
    /// </summary>
    public Guid AssignedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the role was assigned.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional expiration timestamp.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the role.
    /// </summary>
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;
}
