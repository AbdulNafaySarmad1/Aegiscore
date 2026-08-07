using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AegisDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByExternalIdAsync(string provider, string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.AuthProvider.ToString() == provider && u.ExternalId == externalId, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByStatusAsync(UserStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(u => u.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }
}

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(AegisDbContext context) : base(context) { }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
    }
}

public class PermissionRepository : Repository<Permission>, IPermissionRepository
{
    public PermissionRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.Module == module).ToListAsync(cancellationToken);
    }

    public async Task<Permission?> GetByResourceAndActionAsync(string resource, string action, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action, cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> GetByRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.RolePermissions.Any(rp => rp.RoleId == roleId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, List<Guid>>> GetByRolesAsync(List<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var mappings = await _context.Set<RolePermission>()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(cancellationToken);

        return mappings
            .GroupBy(m => m.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.PermissionId).ToList());
    }
}

public class UserRoleRepository : Repository<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<UserRole>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserRole?> GetByUserAndRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
    }
}

public class SamlConfigurationRepository : Repository<SamlConfiguration>, ISamlConfigurationRepository
{
    public SamlConfigurationRepository(AegisDbContext context) : base(context) { }

    public async Task<SamlConfiguration?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
    }

    public async Task<SamlConfiguration?> GetByEntityIdAsync(string entityId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.EntityId == entityId, cancellationToken);
    }
}
