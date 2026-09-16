using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.RuleEngine;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.RuleEngine;

public class RuleEngineService : IRuleEngineService
{
    private readonly IRuleDefinitionRepository _ruleRepo;
    private readonly IRuleVersionRepository _versionRepo;
    private readonly IDecisionTableRepository _tableRepo;
    private readonly ILogger<RuleEngineService> _logger;

    public RuleEngineService(
        IRuleDefinitionRepository ruleRepo,
        IRuleVersionRepository versionRepo,
        IDecisionTableRepository tableRepo,
        ILogger<RuleEngineService> logger)
    {
        _ruleRepo = ruleRepo;
        _versionRepo = versionRepo;
        _tableRepo = tableRepo;
        _logger = logger;
    }

    public async Task<RuleDefinition> CreateRuleAsync(RuleDefinition rule, List<RuleCondition> conditions, List<RuleAction> actions, CancellationToken cancellationToken = default)
    {
        rule.ConditionsJson = JsonSerializer.Serialize(conditions);
        rule.ActionsJson = JsonSerializer.Serialize(actions);
        await _ruleRepo.AddAsync(rule, cancellationToken);

        var version = new RuleVersion { Id = Guid.NewGuid(), RuleId = rule.Id, VersionNumber = 1, ConditionsJson = rule.ConditionsJson, ActionsJson = rule.ActionsJson, ChangeDescription = "Initial version." };
        await _versionRepo.AddAsync(version, cancellationToken);

        return rule;
    }

    public async Task<RuleEvaluationResult> EvaluateRulesAsync(RuleDomain domain, Dictionary<string, object?> facts, CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepo.GetActiveByDomainAsync(domain, cancellationToken);
        var result = new RuleEvaluationResult { TotalRules = rules.Count, ModifiedFacts = new Dictionary<string, object?>(facts) };

        foreach (var rule in rules)
        {
            var conditions = JsonSerializer.Deserialize<List<RuleCondition>>(rule.ConditionsJson) ?? new();
            if (!EvaluateConditions(conditions, facts)) continue;

            var actions = JsonSerializer.Deserialize<List<RuleAction>>(rule.ActionsJson) ?? new();
            result.Matches.Add(new RuleMatch { RuleId = rule.Id, RuleName = rule.Name, Priority = rule.Priority });
            result.MatchedRules++;

            foreach (var action in actions)
            {
                result.ExecutedActions.Add(action);
                if (action.Type == "SetField" && action.TargetField != null)
                    result.ModifiedFacts[action.TargetField] = action.Value;
            }

            if (rule.MatchStrategy == RuleMatchStrategy.FirstMatch) break;
        }

        return result;
    }

    public async Task<RuleSimulationResult> SimulateRuleAsync(Guid ruleId, Dictionary<string, object?> facts, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepo.GetByIdAsync(ruleId, cancellationToken) ?? throw new KeyNotFoundException("Rule not found.");
        var conditions = JsonSerializer.Deserialize<List<RuleCondition>>(rule.ConditionsJson) ?? new();
        var actions = JsonSerializer.Deserialize<List<RuleAction>>(rule.ActionsJson) ?? new();

        var condResults = new List<ConditionEvalResult>();
        var wouldFire = EvaluateConditionsDetailed(conditions, facts, condResults);

        return new RuleSimulationResult { RuleId = ruleId, RuleName = rule.Name, WouldFire = wouldFire, ConditionResults = condResults, ResultingActions = wouldFire ? actions : new() };
    }

    public async Task<DecisionTableResult> EvaluateDecisionTableAsync(Guid tableId, Dictionary<string, object?> facts, CancellationToken cancellationToken = default)
    {
        var table = await _tableRepo.GetByIdAsync(tableId, cancellationToken) ?? throw new KeyNotFoundException("Decision table not found.");
        var inputCols = JsonSerializer.Deserialize<List<DecisionTableColumn>>(table.InputColumnsJson) ?? new();
        var rows = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(table.RowsJson) ?? new();

        var result = new DecisionTableResult { TableId = tableId, TableName = table.Name };

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var allMatch = true;
            var matchedCols = new List<string>();

            foreach (var col in inputCols)
            {
                if (!row.ContainsKey(col.Name)) continue;
                var expected = row[col.Name];
                var actual = facts.TryGetValue(col.Name, out var v) ? v?.ToString() ?? "" : "";

                if (string.Equals(expected, actual?.ToString(), StringComparison.OrdinalIgnoreCase))
                    matchedCols.Add(col.Name);
                else
                    allMatch = false;
            }

            result.MatchedRows.Add(new DecisionTableRowResult { RowIndex = i, Matched = allMatch, MatchedColumns = matchedCols });
            if (allMatch)
            {
                var outputCols = JsonSerializer.Deserialize<List<DecisionTableColumn>>(table.OutputColumnsJson) ?? new();
                foreach (var col in outputCols)
                {
                    if (row.ContainsKey(col.Name))
                        result.OutputValues[col.Name] = row[col.Name];
                }
                break;
            }
        }

        return result;
    }

    public async Task<RuleDefinition> UpdateRuleAsync(Guid ruleId, List<RuleCondition> conditions, List<RuleAction> actions, Guid userId, string? userName, CancellationToken cancellationToken = default)
    {
        var rule = await _ruleRepo.GetByIdAsync(ruleId, cancellationToken) ?? throw new KeyNotFoundException("Rule not found.");
        var newVersion = rule.CurrentVersion + 1;

        rule.ConditionsJson = JsonSerializer.Serialize(conditions);
        rule.ActionsJson = JsonSerializer.Serialize(actions);
        rule.CurrentVersion = newVersion;
        rule.UpdatedAt = DateTime.UtcNow;
        await _ruleRepo.UpdateAsync(rule, cancellationToken);

        var version = new RuleVersion { Id = Guid.NewGuid(), RuleId = ruleId, VersionNumber = newVersion, ConditionsJson = rule.ConditionsJson, ActionsJson = rule.ActionsJson, ChangedByUserId = userId, ChangedByName = userName, ChangeDescription = $"Updated to v{newVersion}." };
        await _versionRepo.AddAsync(version, cancellationToken);

        return rule;
    }

    private static bool EvaluateConditions(List<RuleCondition> conditions, Dictionary<string, object?> facts)
    {
        if (conditions.Count == 0) return true;
        var result = true;
        var currentOp = "And";

        foreach (var condition in conditions)
        {
            bool matches;
            if (condition.Conditions != null && condition.Conditions.Count > 0)
                matches = EvaluateConditions(condition.Conditions, facts);
            else
                matches = EvaluateSingleCondition(condition, facts);

            result = currentOp == "Or" ? result || matches : result && matches;
            currentOp = condition.LogicalOperator;
        }

        return result;
    }

    private static bool EvaluateConditionsDetailed(List<RuleCondition> conditions, Dictionary<string, object?> facts, List<ConditionEvalResult> results)
    {
        var result = true;
        var currentOp = "And";

        foreach (var condition in conditions)
        {
            bool matches;
            if (condition.Conditions != null && condition.Conditions.Count > 0)
                matches = EvaluateConditionsDetailed(condition.Conditions, facts, results);
            else
                matches = EvaluateSingleCondition(condition, facts);

            results.Add(new ConditionEvalResult { Condition = $"{condition.Field} {condition.Operator} {condition.Value}", Passed = matches, ActualValue = facts.TryGetValue(condition.Field, out var v) ? v?.ToString() : null });
            result = currentOp == "Or" ? result || matches : result && matches;
            currentOp = condition.LogicalOperator;
        }

        return result;
    }

    private static bool EvaluateSingleCondition(RuleCondition condition, Dictionary<string, object?> facts)
    {
        var actualValue = facts.TryGetValue(condition.Field, out var v) ? v?.ToString() ?? "" : "";
        var expected = condition.Value;

        return condition.Operator switch
        {
            "Equals" or "==" => string.Equals(actualValue, expected, StringComparison.OrdinalIgnoreCase),
            "NotEquals" or "!=" => !string.Equals(actualValue, expected, StringComparison.OrdinalIgnoreCase),
            "Contains" => (actualValue ?? "").Contains(expected, StringComparison.OrdinalIgnoreCase),
            "GreaterThan" or ">" => decimal.TryParse(actualValue, out var a) && decimal.TryParse(expected, out var b) && a > b,
            "LessThan" or "<" => decimal.TryParse(actualValue, out var a) && decimal.TryParse(expected, out var b) && a < b,
            "GreaterThanOrEqual" or ">=" => decimal.TryParse(actualValue, out var a) && decimal.TryParse(expected, out var b) && a >= b,
            "LessThanOrEqual" or "<=" => decimal.TryParse(actualValue, out var a) && decimal.TryParse(expected, out var b) && a <= b,
            "IsEmpty" => string.IsNullOrEmpty(actualValue),
            "IsNotEmpty" => !string.IsNullOrEmpty(actualValue),
            "In" => expected.Split(',').Select(x => x.Trim()).Contains(actualValue, StringComparer.OrdinalIgnoreCase),
            _ => false
        };
    }
}
