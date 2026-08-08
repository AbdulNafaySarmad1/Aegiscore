using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Identity;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IPermissionRepository _permissionRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IPermissionRepository permissionRepo,
        IUserRoleRepository userRoleRepo,
        ILogger<IdentityService> logger)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _permissionRepo = permissionRepo;
        _userRoleRepo = userRoleRepo;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(string username, string email, string firstName, string lastName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepo.GetByUsernameAsync(username, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"User '{username}' already exists.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            TenantId = tenantId,
            Status = UserStatus.Active,
            AuthProvider = AuthProvider.Local
        };

        await _userRepo.AddAsync(user, cancellationToken);
        _logger.LogInformation("User created: {Username} ({Email})", username, email);

        return user;
    }

    public async Task<User> UpsertUserFromSamlAsync(string externalId, string email, string firstName, string lastName, Dictionary<string, string> claims, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByExternalIdAsync("Saml", externalId, cancellationToken);

        if (user != null)
        {
            user.Email = email;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.LastLoginAt = DateTime.UtcNow;
            user.ExternalClaimsJson = System.Text.Json.JsonSerializer.Serialize(claims);
            await _userRepo.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("User updated via SAML: {ExternalId}", externalId);
            return user;
        }

        user = new User
        {
            Id = Guid.NewGuid(),
            Username = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            AuthProvider = AuthProvider.Saml,
            ExternalId = externalId,
            ExternalClaimsJson = System.Text.Json.JsonSerializer.Serialize(claims),
            TenantId = tenantId,
            Status = UserStatus.Active
        };

        await _userRepo.AddAsync(user, cancellationToken);
        _logger.LogInformation("User created via SAML: {ExternalId} ({Email})", externalId, email);

        return user;
    }

    public async Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        user.Status = UserStatus.Inactive;
        user.DeactivatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("User deactivated: {UserId}", userId);
    }

    public async Task SuspendUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        user.Status = UserStatus.Suspended;
        user.DeactivatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("User suspended: {UserId}", userId);
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, Guid assignedByUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var role = await _roleRepo.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var existing = await _userRoleRepo.GetByUserAndRoleAsync(userId, roleId, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"User already has role '{role.Name}'.");

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = DateTime.UtcNow
        };

        await _userRoleRepo.AddAsync(userRole, cancellationToken);
        _logger.LogInformation("Role '{RoleName}' assigned to user {UserId}", role.Name, userId);
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _userRoleRepo.GetByUserAndRoleAsync(userId, roleId, cancellationToken)
            ?? throw new InvalidOperationException("User does not have this role.");

        await _userRoleRepo.DeleteAsync(userRole, cancellationToken);
        _logger.LogInformation("Role removed from user {UserId}", userId);
    }

    public async Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userRoles = await _userRoleRepo.GetByUserAsync(userId, cancellationToken);

        var permissions = new HashSet<Permission>();
        var now = DateTime.UtcNow;
        foreach (var userRole in userRoles.Where(ur => ur.ExpiresAt == null || ur.ExpiresAt > now))
        {
            if (userRole.Role?.RolePermissions == null) continue;
            foreach (var rp in userRole.Role.RolePermissions)
            {
                if (rp.Permission != null)
                    permissions.Add(rp.Permission);
            }
        }

        return permissions.ToList();
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default)
    {
        var userRoles = await _userRoleRepo.GetByUserAsync(userId, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var userRole in userRoles.Where(ur => ur.ExpiresAt == null || ur.ExpiresAt > now))
        {
            if (userRole.Role?.RolePermissions == null) continue;
            foreach (var rp in userRole.Role.RolePermissions)
            {
                if (rp.Permission != null &&
                    rp.Permission.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase) &&
                    rp.Permission.Action.Equals(action, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
