using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class ExtensionRepository : Repository<Extension>, IExtensionRepository
{
    public ExtensionRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Extension>> GetByCategoryAsync(ExtensionCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Category == category && e.Status == ExtensionStatus.Published).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Extension>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Status == ExtensionStatus.Published).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Extension>> GetByDeveloperAsync(string developerEmail, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.DeveloperEmail == developerEmail).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Extension>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var lower = query.ToLowerInvariant();
        return await _dbSet.Where(e =>
            e.Status == ExtensionStatus.Published &&
            (e.Name.ToLower().Contains(lower) || e.Description.ToLower().Contains(lower))
        ).ToListAsync(cancellationToken);
    }

    public async Task<Extension?> GetByNameAndVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Name == name && e.Version == version, cancellationToken);
    }
}

public class ExtensionInstallationRepository : Repository<ExtensionInstallation>, IExtensionInstallationRepository
{
    public ExtensionInstallationRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ExtensionInstallation>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Extension)
            .Where(i => i.TenantId == tenantId && i.Status != InstallationStatus.Uninstalled)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExtensionInstallation?> GetByTenantAndExtensionAsync(Guid tenantId, Guid extensionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.ExtensionId == extensionId && i.Status != InstallationStatus.Uninstalled, cancellationToken);
    }

    public async Task<IReadOnlyList<ExtensionInstallation>> GetByExtensionAsync(Guid extensionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.ExtensionId == extensionId).ToListAsync(cancellationToken);
    }
}

public class ExtensionReviewRepository : Repository<ExtensionReview>, IExtensionReviewRepository
{
    public ExtensionReviewRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ExtensionReview>> GetByExtensionAsync(Guid extensionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.ExtensionId == extensionId).OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExtensionReview>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<double> GetAverageRatingAsync(Guid extensionId, CancellationToken cancellationToken = default)
    {
        var ratings = await _dbSet.Where(r => r.ExtensionId == extensionId).Select(r => (double)r.Rating).ToListAsync(cancellationToken);
        return ratings.Count > 0 ? ratings.Average() : 0;
    }
}
