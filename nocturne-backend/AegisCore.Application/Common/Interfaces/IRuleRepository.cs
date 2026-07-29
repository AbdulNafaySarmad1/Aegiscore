using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IRuleDefinitionRepository : IRepository<RuleDefinition>
{
    Task<IReadOnlyList<RuleDefinition>> GetByDomainAsync(RuleDomain domain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuleDefinition>> GetActiveByDomainAsync(RuleDomain domain, CancellationToken cancellationToken = default);
    Task<RuleDefinition?> GetWithVersionsAsync(Guid ruleId, CancellationToken cancellationToken = default);
}

public interface IRuleVersionRepository : IRepository<RuleVersion>
{
    Task<IReadOnlyList<RuleVersion>> GetByRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);
    Task<RuleVersion?> GetByRuleAndVersionAsync(Guid ruleId, int version, CancellationToken cancellationToken = default);
}

public interface IDecisionTableRepository : IRepository<DecisionTable>
{
    Task<IReadOnlyList<DecisionTable>> GetByDomainAsync(RuleDomain domain, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DecisionTable>> GetActiveAsync(CancellationToken cancellationToken = default);
}
