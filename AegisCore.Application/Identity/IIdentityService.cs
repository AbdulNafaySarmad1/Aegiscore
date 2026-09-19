using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Identity;

public interface IIdentityService
{
    Task<User> CreateUserAsync(string username, string email, string firstName, string lastName, Guid tenantId, CancellationToken cancellationToken = default);
    Task<User> UpsertUserFromSamlAsync(string externalId, string email, string firstName, string lastName, Dictionary<string, string> claims, Guid tenantId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Finds the person by (tenant, issuer, subject) and refreshes their profile, or creates them
    /// without a password. Never matches on subject alone: two directories can issue the same one.
    /// </summary>
    Task<User> UpsertUserFromExternalIdentityAsync(ExternalIdentity identity, Guid tenantId, AuthProvider provider, CancellationToken cancellationToken = default);
    Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SuspendUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(Guid userId, Guid roleId, Guid assignedByUserId, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default);
}
