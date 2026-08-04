namespace AegisCore.Application.Common.Interfaces;

/// <summary>
/// Service for audit logging of critical actions.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs a critical action.
    /// </summary>
    /// <param name="action">The action performed (CREATE, UPDATE, DELETE, LOGIN, etc.).</param>
    /// <param name="resourceType">The type of resource affected.</param>
    /// <param name="resourceId">The identifier of the resource affected.</param>
    /// <param name="details">Additional details about the action.</param>
    /// <param name="success">Whether the action was successful.</param>
    Task LogActionAsync(string action, string resourceType, Guid resourceId, string? details = null, bool success = true);
}
