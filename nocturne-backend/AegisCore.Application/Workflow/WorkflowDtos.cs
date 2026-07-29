namespace AegisCore.Application.Workflow;

public class WorkflowStartRequest
{
    public Guid DefinitionId { get; set; }
    public string? InstanceName { get; set; }
    public Dictionary<string, object?>? Context { get; set; }
}

public class ApprovalRequest
{
    public Guid ApprovalId { get; set; }
    public string? Comment { get; set; }
}

public class WorkflowInstanceDto
{
    public Guid Id { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string DefinitionName { get; set; } = string.Empty;
    public int DefinitionVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentNodeId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<WorkflowStepDto> Steps { get; set; } = new();
}

public class WorkflowStepDto
{
    public Guid Id { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeLabel { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ApprovalRecordDto> Approvals { get; set; } = new();
}

public class ApprovalRecordDto
{
    public Guid Id { get; set; }
    public Guid ApproverUserId { get; set; }
    public string ApproverRole { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public int Sequence { get; set; }
    public DateTime? DecidedAt { get; set; }
    public bool IsEscalated { get; set; }
}

public class ApprovalConfig
{
    public string Type { get; set; } = "Single";
    public List<ApproverConfig> Approvers { get; set; } = new();
    public int TimeoutHours { get; set; } = 48;
    public string? EscalationUserId { get; set; }
    public string? RejectTransitionId { get; set; }
}

public class ApproverConfig
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public int Sequence { get; set; }
}
