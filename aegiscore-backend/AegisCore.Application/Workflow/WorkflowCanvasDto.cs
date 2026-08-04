namespace AegisCore.Application.Workflow;

public class WorkflowCanvasDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<NodeDefinitionDto> Nodes { get; set; } = new();
    public List<TransitionDefinitionDto> Transitions { get; set; } = new();
}

public class NodeDefinitionDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public NodeConfigDto? Config { get; set; }
}

public class NodeConfigDto
{
    public string? ActionType { get; set; }
    public string? EntityType { get; set; }
    public string? FieldName { get; set; }
    public string? FieldValue { get; set; }
    public string? TemplateId { get; set; }
    public string? ApiUrl { get; set; }
    public string? ApiMethod { get; set; }
    public NotificationConfigDto? Notification { get; set; }
    public ApprovalConfigDto? Approval { get; set; }
    public ConditionConfigDto? Condition { get; set; }
    public int? WaitSeconds { get; set; }
    public string? CronExpression { get; set; }
}

public class NotificationConfigDto
{
    public string Channel { get; set; } = "Email";
    public string RecipientExpression { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}

public class ApprovalConfigDto
{
    public string Type { get; set; } = "Single";
    public List<ApproverConfigDto> Approvers { get; set; } = new();
    public int TimeoutHours { get; set; } = 48;
    public string? EscalationUserId { get; set; }
    public string? RejectTransitionId { get; set; }
}

public class ApproverConfigDto
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public int Sequence { get; set; }
}

public class ConditionConfigDto
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "Equals";
    public string Value { get; set; } = string.Empty;
    public string LogicalOperator { get; set; } = "And";
    public List<ConditionConfigDto>? Conditions { get; set; }
}

public class TransitionDefinitionDto
{
    public string Id { get; set; } = string.Empty;
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? ConditionExpression { get; set; }
}
