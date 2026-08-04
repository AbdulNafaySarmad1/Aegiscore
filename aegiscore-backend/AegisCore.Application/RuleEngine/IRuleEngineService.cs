using AegisCore.Domain.Entities;

namespace AegisCore.Application.RuleEngine;

public interface IRuleEngineService
{
    Task<RuleDefinition> CreateRuleAsync(RuleDefinition rule, List<RuleCondition> conditions, List<RuleAction> actions, CancellationToken cancellationToken = default);
    Task<RuleEvaluationResult> EvaluateRulesAsync(RuleDomain domain, Dictionary<string, object?> facts, CancellationToken cancellationToken = default);
    Task<RuleSimulationResult> SimulateRuleAsync(Guid ruleId, Dictionary<string, object?> facts, CancellationToken cancellationToken = default);
    Task<DecisionTableResult> EvaluateDecisionTableAsync(Guid tableId, Dictionary<string, object?> facts, CancellationToken cancellationToken = default);
    Task<RuleDefinition> UpdateRuleAsync(Guid ruleId, List<RuleCondition> conditions, List<RuleAction> actions, Guid userId, string? userName, CancellationToken cancellationToken = default);
}
