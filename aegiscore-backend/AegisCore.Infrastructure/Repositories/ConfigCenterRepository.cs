using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class ConfigurationEntryRepository : Repository<ConfigurationEntry>, IConfigurationEntryRepository
{
    public ConfigurationEntryRepository(AegisDbContext context) : base(context) { }

    public async Task<ConfigurationEntry?> GetByKeyAsync(string key, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(e => e.Versions).Where(e => e.Key == key);
        if (tenantId.HasValue) query = query.Where(e => e.TenantId == tenantId.Value);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationEntry>> GetByDomainAsync(ConfigurationDomain domain, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Domain == domain && e.Status == ConfigEntryStatus.Active).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationEntry>> GetByEnvironmentAsync(ConfigEnvironment environment, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Environment == environment).ToListAsync(cancellationToken);
    }
}

public class ConfigVersionRepository : Repository<ConfigVersion>, IConfigVersionRepository
{
    public ConfigVersionRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ConfigVersion>> GetByEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.ConfigurationEntryId == entryId).OrderByDescending(v => v.VersionNumber).ToListAsync(cancellationToken);
    }

    public async Task<ConfigVersion?> GetByEntryAndVersionAsync(Guid entryId, int versionNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(v => v.ConfigurationEntryId == entryId && v.VersionNumber == versionNumber, cancellationToken);
    }
}

public class ConfigAuditLogRepository : Repository<ConfigAuditLog>, IConfigAuditLogRepository
{
    public ConfigAuditLogRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ConfigAuditLog>> GetByEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(l => l.ConfigurationEntryId == entryId).OrderByDescending(l => l.ChangedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigAuditLog>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(l => l.ChangedByUserId == userId).OrderByDescending(l => l.ChangedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigAuditLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(l => l.ChangedAt >= from && l.ChangedAt <= to).OrderByDescending(l => l.ChangedAt).ToListAsync(cancellationToken);
    }
}
