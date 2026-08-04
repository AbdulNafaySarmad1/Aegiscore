namespace AegisCore.Domain.Common;

/// <summary>
/// Interface for entities that are scoped to a specific tenant.
/// </summary>
public interface ITenantScoped
{
    /// <summary>
    /// The tenant identifier.
    /// </summary>
    Guid TenantId { get; set; }
}
