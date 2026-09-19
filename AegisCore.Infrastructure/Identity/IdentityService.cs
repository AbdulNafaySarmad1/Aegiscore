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

    public async Task<User> UpsertUserFromExternalIdentityAsync(ExternalIdentity identity, Guid tenantId, AuthProvider provider, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("A tenant is required to match an external identity.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(identity.Issuer) || string.IsNullOrWhiteSpace(identity.Subject))
            throw new ArgumentException("An external identity needs both an issuer and a subject.", nameof(identity));
        if (string.IsNullOrWhiteSpace(identity.Email))
            throw new ExternalSignInException("email_missing", "The identity provider did not say who you are: no email address was returned. Ask your administrator to release the email claim for this application.");

        var email = identity.Email.Trim().ToLowerInvariant();
        var claimsJson = ClaimsJson(identity);

        // The identity is (tenant, issuer, subject) — the audit's collision: the same "oid" from
        // two Entra directories must land on two different people.
        var user = await _userRepo.GetByExternalIdentityAsync(tenantId, identity.Issuer, identity.Subject, cancellationToken);
        if (user is not null)
        {
            user.Email = email;
            if (!string.IsNullOrWhiteSpace(identity.FirstName)) user.FirstName = identity.FirstName;
            if (!string.IsNullOrWhiteSpace(identity.LastName)) user.LastName = identity.LastName;
            user.LastLoginAt = DateTime.UtcNow;
            user.ExternalClaimsJson = claimsJson;
            await _userRepo.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("User signed in via {Provider}: {Issuer}/{Subject}", provider, identity.Issuer, identity.Subject);
            return user;
        }

        // First sign-in through this provider. An account an administrator created ahead of time with
        // the same email (the wizard's administrator, or a person added under Identity) is claimed by
        // this identity rather than duplicated — unless it already belongs to a different issuer,
        // which is a different person or a misconfiguration, and is refused rather than merged.
        var byEmail = await _userRepo.GetByTenantAndEmailAsync(tenantId, email, cancellationToken);
        if (byEmail is not null)
        {
            if (!string.IsNullOrWhiteSpace(byEmail.ExternalIssuer) || !string.IsNullOrWhiteSpace(byEmail.ExternalId))
                throw new ExternalSignInException("identity_conflict",
                    $"An account with the address {email} already signs in through a different identity provider. Ask your administrator to review the account.");
            byEmail.ExternalIssuer = identity.Issuer;
            byEmail.ExternalId = identity.Subject;
            // A local account that has a password keeps it: this is the "Local for a few administrator
            // accounts plus Entra for everyone" case, and the same person may use either door.
            if (string.IsNullOrWhiteSpace(byEmail.PasswordHash)) byEmail.AuthProvider = provider;
            if (!string.IsNullOrWhiteSpace(identity.FirstName)) byEmail.FirstName = identity.FirstName;
            if (!string.IsNullOrWhiteSpace(identity.LastName)) byEmail.LastName = identity.LastName;
            byEmail.LastLoginAt = DateTime.UtcNow;
            byEmail.ExternalClaimsJson = claimsJson;
            await _userRepo.UpdateAsync(byEmail, cancellationToken);
            _logger.LogInformation("Existing account {Email} linked to {Provider} identity {Issuer}/{Subject}", email, provider, identity.Issuer, identity.Subject);
            return byEmail;
        }

        // Provisioned just in time. No password hash, ever: PasswordController refuses to set one on
        // an account whose AuthProvider is not Local, and this one never is.
        user = new User
        {
            Id = Guid.NewGuid(),
            Username = email,
            Email = email,
            FirstName = identity.FirstName,
            LastName = identity.LastName,
            AuthProvider = provider,
            ExternalIssuer = identity.Issuer,
            ExternalId = identity.Subject,
            ExternalClaimsJson = claimsJson,
            TenantId = tenantId,
            Status = UserStatus.Active,
            LastLoginAt = DateTime.UtcNow
        };
        await _userRepo.AddAsync(user, cancellationToken);
        _logger.LogInformation("User created via {Provider}: {Issuer}/{Subject} ({Email})", provider, identity.Issuer, identity.Subject, email);
        return user;
    }

    /// <summary>The column holds 1000 characters; keep the claims that identify the person if the rest do not fit.</summary>
    private static string ClaimsJson(ExternalIdentity identity)
    {
        var full = System.Text.Json.JsonSerializer.Serialize(identity.Claims);
        if (full.Length <= 1000) return full;
        var essential = new Dictionary<string, string> { ["iss"] = identity.Issuer, ["sub"] = identity.Subject, ["email"] = identity.Email };
        return System.Text.Json.JsonSerializer.Serialize(essential);
    }

    public async Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        user.Status = UserStatus.Inactive;
        user.DeactivatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("User deactivated: {UserId}", userId);
    }

    public async Task SuspendUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        user.Status = UserStatus.Suspended;
        user.DeactivatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("User suspended: {UserId}", userId);
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, Guid assignedByUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var role = await _roleRepo.GetByIdAsync(roleId, cancellationToken)
            ?? throw new KeyNotFoundException("Role not found.");

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
        foreach (var userRole in userRoles)
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

        foreach (var userRole in userRoles)
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
