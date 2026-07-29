using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class RuleDefinitionRepository : Repository<RuleDefinition>, IRuleDefinitionRepository
{
    public RuleDefinitionRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<RuleDefinition>> GetByDomainAsync(RuleDomain domain, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.Domain == domain).OrderBy(r => r.Priority).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RuleDefinition>> GetActiveByDomainAsync(RuleDomain domain, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet.Where(r =>
            r.Domain == domain && r.Status == RuleStatus.Active &&
            (!r.EffectiveFrom.HasValue || r.EffectiveFrom.Value <= now) &&
            (!r.EffectiveTo.HasValue || r.EffectiveTo.Value >= now)
        ).OrderBy(r => r.Priority).ToListAsync(cancellationToken);
    }

    public async Task<RuleDefinition?> GetWithVersionsAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(r => r.Versions).FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken);
    }
}

public class RuleVersionRepository : Repository<RuleVersion>, IRuleVersionRepository
{
    public RuleVersionRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<RuleVersion>> GetByRuleAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(v => v.RuleId == ruleId).OrderByDescending(v => v.VersionNumber).ToListAsync(cancellationToken);
    }

    public async Task<RuleVersion?> GetByRuleAndVersionAsync(Guid ruleId, int version, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(v => v.RuleId == ruleId && v.VersionNumber == version, cancellationToken);
    }
}

public class DecisionTableRepository : Repository<DecisionTable>, IDecisionTableRepository
{
    public DecisionTableRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<DecisionTable>> GetByDomainAsync(RuleDomain domain, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.Domain == domain).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DecisionTable>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.Status == RuleStatus.Active).ToListAsync(cancellationToken);
    }
}
