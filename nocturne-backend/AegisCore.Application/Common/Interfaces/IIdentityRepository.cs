using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalIdAsync(string provider, string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByStatusAsync(UserStatus status, CancellationToken cancellationToken = default);
    Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
}

public interface IPermissionRepository : IRepository<Permission>
{
    Task<IReadOnlyList<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken = default);
    Task<Permission?> GetByResourceAndActionAsync(string resource, string action, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> GetByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}

public interface IUserRoleRepository : IRepository<UserRole>
{
    Task<IReadOnlyList<UserRole>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserRole?> GetByUserAndRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}

public interface ISamlConfigurationRepository : IRepository<SamlConfiguration>
{
    Task<SamlConfiguration?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SamlConfiguration?> GetByEntityIdAsync(string entityId, CancellationToken cancellationToken = default);
}
