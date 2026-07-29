namespace AegisCore.Application.RuleEngine;

public class RuleCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "Equals";
    public string Value { get; set; } = string.Empty;
    public string LogicalOperator { get; set; } = "And";
    public List<RuleCondition>? Conditions { get; set; }
}

public class RuleAction
{
    public string Type { get; set; } = "SetField";
    public string? TargetField { get; set; }
    public string? Value { get; set; }
    public string? WorkflowId { get; set; }
    public string? NotificationTemplate { get; set; }
    public string? Message { get; set; }
}

public class RuleEvaluationResult
{
    public int TotalRules { get; set; }
    public int MatchedRules { get; set; }
    public List<RuleMatch> Matches { get; set; } = new();
    public List<RuleAction> ExecutedActions { get; set; } = new();
    public Dictionary<string, object?> ModifiedFacts { get; set; } = new();
}

public class RuleMatch
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public List<string> MatchedConditions { get; set; } = new();
}

public class RuleSimulationResult
{
    public Guid RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public bool WouldFire { get; set; }
    public List<ConditionEvalResult> ConditionResults { get; set; } = new();
    public List<RuleAction> ResultingActions { get; set; } = new();
}

public class ConditionEvalResult
{
    public string Condition { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? ActualValue { get; set; }
}

public class DecisionTableResult
{
    public Guid TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public List<DecisionTableRowResult> MatchedRows { get; set; } = new();
    public Dictionary<string, object?> OutputValues { get; set; } = new();
}

public class DecisionTableRowResult
{
    public int RowIndex { get; set; }
    public bool Matched { get; set; }
    public List<string> MatchedColumns { get; set; } = new();
}

public class RuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int CurrentVersion { get; set; }
    public List<RuleCondition> Conditions { get; set; } = new();
    public List<RuleAction> Actions { get; set; } = new();
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class DecisionTableDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<DecisionTableColumn> InputColumns { get; set; } = new();
    public List<DecisionTableColumn> OutputColumns { get; set; } = new();
    public List<Dictionary<string, string>> Rows { get; set; } = new();
}

public class DecisionTableColumn
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? Operator { get; set; }
}

public class CreateRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Domain { get; set; } = "Admissions";
    public int Priority { get; set; }
    public List<RuleCondition> Conditions { get; set; } = new();
    public List<RuleAction> Actions { get; set; } = new();
}

public class EvaluateFactsRequest
{
    public Dictionary<string, object?> Facts { get; set; } = new();
}
