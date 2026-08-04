using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

/// <summary>
/// Represents the association between a role and a permission.
/// </summary>
[Table("RolePermissions")]
public class RolePermission : ITenantScoped
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Role identifier.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Permission identifier.
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Navigation property to the role.
    /// </summary>
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Navigation property to the permission.
    /// </summary>
    [ForeignKey(nameof(PermissionId))]
    public Permission Permission { get; set; } = null!;
}
