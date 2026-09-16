using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Workflow;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Workflow;

public class WorkflowEngine : IWorkflowEngine
{
    private readonly IWorkflowDefinitionRepository _definitionRepo;
    private readonly IWorkflowInstanceRepository _instanceRepo;
    private readonly IWorkflowStepRepository _stepRepo;
    private readonly IApprovalService _approvalService;
    private readonly INotificationService _notificationService;
    private readonly INotificationRecordRepository _notificationRecordRepo;
    private readonly ILogger<WorkflowEngine> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public WorkflowEngine(
        IWorkflowDefinitionRepository definitionRepo,
        IWorkflowInstanceRepository instanceRepo,
        IWorkflowStepRepository stepRepo,
        IApprovalService approvalService,
        INotificationService notificationService,
        INotificationRecordRepository notificationRecordRepo,
        ILogger<WorkflowEngine> logger)
    {
        _definitionRepo = definitionRepo;
        _instanceRepo = instanceRepo;
        _stepRepo = stepRepo;
        _approvalService = approvalService;
        _notificationService = notificationService;
        _notificationRecordRepo = notificationRecordRepo;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task<WorkflowInstance> StartWorkflowAsync(Guid definitionId, Guid triggeredByUserId, Dictionary<string, object?>? context = null, CancellationToken cancellationToken = default)
    {
        var definition = await _definitionRepo.GetByIdAsync(definitionId, cancellationToken)
            ?? throw new KeyNotFoundException("Workflow definition not found.");

        if (definition.Status != WorkflowDefinitionStatus.Published)
            throw new InvalidOperationException("Cannot start a workflow that is not published.");

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            DefinitionId = definitionId,
            DefinitionVersion = definition.Version,
            InstanceName = $"{definition.Name} - {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
            TriggeredByUserId = triggeredByUserId,
            TriggerType = definition.TriggerType,
            ContextJson = JsonSerializer.Serialize(context ?? new Dictionary<string, object?>(), _jsonOptions),
            VariablesJson = "{}",
            Status = WorkflowInstanceStatus.Running,
            TenantId = definition.TenantId
        };

        instance = await _instanceRepo.AddAsync(instance, cancellationToken);

        var nodes = ParseNodes(definition.NodesJson);
        var startNode = nodes.FirstOrDefault(n => n.Type == "Start")
            ?? throw new InvalidOperationException("Workflow has no Start node.");

        await ExecuteNodeAsync(instance.Id, startNode, nodes, ParseTransitions(definition.TransitionsJson), cancellationToken);

        return instance;
    }

    public async Task AdvanceWorkflowAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Instance not found.");

        if (instance.Status != WorkflowInstanceStatus.Running)
            return;

        var definition = await _definitionRepo.GetByIdAsync(instance.DefinitionId, cancellationToken)
            ?? throw new KeyNotFoundException("Definition not found.");

        var nodes = ParseNodes(definition.NodesJson);
        var transitions = ParseTransitions(definition.TransitionsJson);
        var currentNode = nodes.FirstOrDefault(n => n.Id == instance.CurrentNodeId);

        if (currentNode == null) return;

        var outgoingTransitions = transitions.Where(t => t.SourceNodeId == instance.CurrentNodeId).ToList();

        NodeDefinitionDto? nextNode = null;
        if (outgoingTransitions.Count == 1)
        {
            nextNode = nodes.FirstOrDefault(n => n.Id == outgoingTransitions[0].TargetNodeId);
            if (nextNode != null)
                await ExecuteNodeAsync(instanceId, nextNode, nodes, transitions, cancellationToken);
        }
        else
        {
            if (outgoingTransitions.Count > 1)
            {
                var conditionResult = EvaluateConditionalTransition(outgoingTransitions, instance, cancellationToken);
                if (conditionResult != null)
                {
                    nextNode = nodes.FirstOrDefault(n => n.Id == conditionResult);
                    if (nextNode != null)
                        await ExecuteNodeAsync(instanceId, nextNode, nodes, transitions, cancellationToken);
                }
            }
        }
    }

    public async Task CancelWorkflowAsync(Guid instanceId, string reason, CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Instance not found.");

        instance.Status = WorkflowInstanceStatus.Cancelled;
        instance.CompletedAt = DateTime.UtcNow;
        instance.ErrorMessage = reason;
        await _instanceRepo.UpdateAsync(instance, cancellationToken);

        _logger.LogInformation("Workflow {InstanceId} cancelled: {Reason}", instanceId, reason);
    }

    public async Task SuspendWorkflowAsync(Guid instanceId, string reason, CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Instance not found.");

        instance.Status = WorkflowInstanceStatus.Suspended;
        instance.ErrorMessage = reason;
        await _instanceRepo.UpdateAsync(instance, cancellationToken);
    }

    public async Task ResumeWorkflowAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Instance not found.");

        if (instance.Status != WorkflowInstanceStatus.Suspended)
            throw new InvalidOperationException("Only suspended workflows can be resumed.");

        instance.Status = WorkflowInstanceStatus.Running;
        instance.ErrorMessage = null;
        await _instanceRepo.UpdateAsync(instance, cancellationToken);

        await AdvanceWorkflowAsync(instanceId, cancellationToken);
    }

    public async Task RetryFailedStepAsync(Guid stepId, CancellationToken cancellationToken = default)
    {
        var step = await _stepRepo.GetByIdAsync(stepId, cancellationToken)
            ?? throw new KeyNotFoundException("Step not found.");

        if (step.Status != WorkflowStepStatus.Failed)
            throw new InvalidOperationException("Only failed steps can be retried.");

        step.Status = WorkflowStepStatus.InProgress;
        step.ErrorMessage = null;
        step.RetryCount++;
        step.StartedAt = DateTime.UtcNow;
        await _stepRepo.UpdateAsync(step, cancellationToken);

        var instance = await _instanceRepo.GetByIdAsync(step.InstanceId, cancellationToken);
        if (instance != null)
        {
            instance.Status = WorkflowInstanceStatus.Running;
            await _instanceRepo.UpdateAsync(instance, cancellationToken);
            await AdvanceWorkflowAsync(step.InstanceId, cancellationToken);
        }
    }

    private async Task ExecuteNodeAsync(Guid instanceId, NodeDefinitionDto node, List<NodeDefinitionDto> allNodes, List<TransitionDefinitionDto> transitions, CancellationToken cancellationToken)
    {
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Instance not found.");

        instance.CurrentNodeId = node.Id;
        await _instanceRepo.UpdateAsync(instance, cancellationToken);

        var step = new WorkflowStep
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            NodeId = node.Id,
            NodeLabel = node.Label,
            NodeType = ParseNodeType(node.Type),
            Status = WorkflowStepStatus.InProgress,
            Sequence = await GetNextSequenceAsync(instanceId, cancellationToken),
            StartedAt = DateTime.UtcNow
        };

        step = await _stepRepo.AddAsync(step, cancellationToken);

        try
        {
            switch (node.Type)
            {
                case "Start":
                    step.Status = WorkflowStepStatus.Completed;
                    step.CompletedAt = DateTime.UtcNow;
                    await _stepRepo.UpdateAsync(step, cancellationToken);
                    await MoveToNextNodeAsync(instanceId, node.Id, allNodes, transitions, cancellationToken);
                    break;

                case "End":
                    step.Status = WorkflowStepStatus.Completed;
                    step.CompletedAt = DateTime.UtcNow;
                    await _stepRepo.UpdateAsync(step, cancellationToken);
                    await CompleteInstanceAsync(instanceId, cancellationToken);
                    break;

                case "Approval":
                    await ExecuteApprovalNodeAsync(instanceId, step, node, allNodes, transitions, cancellationToken);
                    break;

                case "Notification":
                    await ExecuteNotificationNodeAsync(instanceId, step, node, allNodes, transitions, cancellationToken);
                    break;

                case "Wait":
                    await ExecuteWaitNodeAsync(instanceId, step, node, allNodes, transitions, cancellationToken);
                    break;

                case "Condition":
                    step.Status = WorkflowStepStatus.Completed;
                    step.CompletedAt = DateTime.UtcNow;
                    await _stepRepo.UpdateAsync(step, cancellationToken);
                    await AdvanceWorkflowAsync(instanceId, cancellationToken);
                    break;

                default:
                    await ExecuteActionNodeAsync(instanceId, step, node, allNodes, transitions, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Node execution failed: {NodeId} ({NodeLabel}) in instance {InstanceId}", node.Id, node.Label, instanceId);
            step.Status = WorkflowStepStatus.Failed;
            step.ErrorMessage = "Step execution failed.";
            step.CompletedAt = DateTime.UtcNow;
            await _stepRepo.UpdateAsync(step, cancellationToken);

            instance.Status = WorkflowInstanceStatus.Failed;
            instance.ErrorMessage = $"Failed at node '{node.Label}'.";
            await _instanceRepo.UpdateAsync(instance, cancellationToken);
        }
    }

    private async Task ExecuteApprovalNodeAsync(Guid instanceId, WorkflowStep step, NodeDefinitionDto node, List<NodeDefinitionDto> allNodes, List<TransitionDefinitionDto> transitions, CancellationToken cancellationToken)
    {
        if (node.Config?.Approval == null)
            throw new InvalidOperationException("Approval node has no configuration.");

        var approvalConfig = new ApprovalConfig
        {
            Type = node.Config.Approval.Type,
            TimeoutHours = node.Config.Approval.TimeoutHours,
            EscalationUserId = node.Config.Approval.EscalationUserId,
            RejectTransitionId = node.Config.Approval.RejectTransitionId,
            Approvers = node.Config.Approval.Approvers.Select(a => new ApproverConfig
            {
                UserId = a.UserId,
                Role = a.Role,
                Sequence = a.Sequence
            }).ToList()
        };

        step.Status = WorkflowStepStatus.AwaitingApproval;
        await _stepRepo.UpdateAsync(step, cancellationToken);

        await _approvalService.InitializeApprovalsForStep(step.Id, approvalConfig.Approvers, cancellationToken);
    }

    private async Task ExecuteNotificationNodeAsync(Guid instanceId, WorkflowStep step, NodeDefinitionDto node, List<NodeDefinitionDto> allNodes, List<TransitionDefinitionDto> transitions, CancellationToken cancellationToken)
    {
        if (node.Config?.Notification == null)
            throw new InvalidOperationException("Notification node has no configuration.");

        var config = node.Config.Notification;
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken);

        var notification = new NotificationRecord
        {
            Id = Guid.NewGuid(),
            InstanceId = instanceId,
            Channel = Enum.Parse<NotificationChannel>(config.Channel),
            Recipient = ResolveExpression(config.RecipientExpression, instance),
            Subject = ResolveExpression(config.SubjectTemplate, instance),
            Body = ResolveExpression(config.BodyTemplate, instance),
            Status = NotificationStatus.Pending
        };

        notification = await _notificationRecordRepo.AddAsync(notification, cancellationToken);
        await _notificationService.SendAsync(notification, cancellationToken);

        step.Status = WorkflowStepStatus.Completed;
        step.CompletedAt = DateTime.UtcNow;
        step.OutputJson = JsonSerializer.Serialize(new { notificationId = notification.Id, status = notification.Status.ToString() });
        await _stepRepo.UpdateAsync(step, cancellationToken);

        await MoveToNextNodeAsync(instanceId, node.Id, allNodes, transitions, cancellationToken);
    }

    private async Task ExecuteWaitNodeAsync(Guid instanceId, WorkflowStep step, NodeDefinitionDto node, List<NodeDefinitionDto> allNodes, List<TransitionDefinitionDto> transitions, CancellationToken cancellationToken)
    {
        var waitSeconds = node.Config?.WaitSeconds ?? 60;
        await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken);

        step.Status = WorkflowStepStatus.Completed;
        step.CompletedAt = DateTime.UtcNow;
        await _stepRepo.UpdateAsync(step, cancellationToken);

        await MoveToNextNodeAsync(instanceId, node.Id, allNodes, transitions, cancellationToken);
    }

    private async Task ExecuteActionNodeAsync(Guid instanceId, WorkflowStep step, NodeDefinitionDto node, List<NodeDefinitionDto> allNodes, List<TransitionDefinitionDto> transitions, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Action node executed: {NodeLabel} ({ActionType})", node.Label, node.Config?.ActionType ?? "unknown");

        step.Status = WorkflowStepStatus.Completed;
        step.CompletedAt = DateTime.UtcNow;
        step.OutputJson = JsonSerializer.Serialize(new { actionType = node.Config?.ActionType, executed = true });
        await _stepRepo.UpdateAsync(step, cancellationToken);

        await MoveToNextNodeAsync(instanceId, node.Id, allNodes, transitions, cancellationToken);
    }

    private async Task MoveToNextNodeAsync(Guid instanceId, string currentNodeId, List<NodeDefinitionDto> allNodes, List<TransitionDefinitionDto> transitions, CancellationToken cancellationToken)
    {
        var outgoing = transitions.FirstOrDefault(t => t.SourceNodeId == currentNodeId);
        if (outgoing == null)
        {
            await CompleteInstanceAsync(instanceId, cancellationToken);
            return;
        }

        var nextNode = allNodes.FirstOrDefault(n => n.Id == outgoing.TargetNodeId);
        if (nextNode != null)
            await ExecuteNodeAsync(instanceId, nextNode, allNodes, transitions, cancellationToken);
    }

    private async Task CompleteInstanceAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken);
        if (instance != null)
        {
            instance.Status = WorkflowInstanceStatus.Completed;
            instance.CompletedAt = DateTime.UtcNow;
            await _instanceRepo.UpdateAsync(instance, cancellationToken);
        }
    }

    public async Task OnApprovalDecidedAsync(Guid stepId, bool allApproved, CancellationToken cancellationToken)
    {
        var step = await _stepRepo.GetByIdAsync(stepId, cancellationToken)
            ?? throw new KeyNotFoundException("Step not found.");

        var definition = await GetDefinitionForInstance(step.InstanceId, cancellationToken);

        if (allApproved)
        {
            step.Status = WorkflowStepStatus.Completed;
            step.CompletedAt = DateTime.UtcNow;
            await _stepRepo.UpdateAsync(step, cancellationToken);

            var nodes = ParseNodes(definition.NodesJson);
            var transitions = ParseTransitions(definition.TransitionsJson);
            var outgoing = transitions.FirstOrDefault(t => t.SourceNodeId == step.NodeId);

            if (outgoing != null)
            {
                var nextNode = nodes.FirstOrDefault(n => n.Id == outgoing.TargetNodeId);
                if (nextNode != null)
                    await ExecuteNodeAsync(step.InstanceId, nextNode, nodes, transitions, cancellationToken);
            }
            else
            {
                await CompleteInstanceAsync(step.InstanceId, cancellationToken);
            }
        }
        else
        {
            var nodeConfig = ParseNodes(definition.NodesJson).FirstOrDefault(n => n.Id == step.NodeId)?.Config;
            var rejectTransitionId = nodeConfig?.Approval?.RejectTransitionId;

            if (!string.IsNullOrEmpty(rejectTransitionId))
            {
                var nodes = ParseNodes(definition.NodesJson);
                var transitions = ParseTransitions(definition.TransitionsJson);
                var rejectNode = nodes.FirstOrDefault(n => n.Id == rejectTransitionId);
                if (rejectNode != null)
                    await ExecuteNodeAsync(step.InstanceId, rejectNode, nodes, transitions, cancellationToken);
            }
            else
            {
                step.Status = WorkflowStepStatus.Failed;
                step.ErrorMessage = "Rejected by approver.";
                await _stepRepo.UpdateAsync(step, cancellationToken);

                await CancelWorkflowAsync(step.InstanceId, "Rejected by approver.", cancellationToken);
            }
        }
    }

    private async Task<WorkflowDefinition> GetDefinitionForInstance(Guid instanceId, CancellationToken cancellationToken)
    {
        var instance = await _instanceRepo.GetByIdAsync(instanceId, cancellationToken)
            ?? throw new KeyNotFoundException("Instance not found.");

        return await _definitionRepo.GetByIdAsync(instance.DefinitionId, cancellationToken)
            ?? throw new KeyNotFoundException("Definition not found.");
    }

    private string? EvaluateConditionalTransition(List<TransitionDefinitionDto> transitions, WorkflowInstance instance, CancellationToken cancellationToken)
    {
        foreach (var transition in transitions)
        {
            if (string.IsNullOrEmpty(transition.ConditionExpression))
                return transition.TargetNodeId;

            if (EvaluateExpression(transition.ConditionExpression, instance))
                return transition.TargetNodeId;
        }
        return transitions.FirstOrDefault(t => string.IsNullOrEmpty(t.ConditionExpression))?.TargetNodeId;
    }

    private bool EvaluateExpression(string expression, WorkflowInstance instance)
    {
        try
        {
            var variables = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(instance.VariablesJson) ?? new();
            var context = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(instance.ContextJson) ?? new();

            var parts = expression.Split(' ');
            if (parts.Length < 3) return false;

            var field = parts[0];
            var op = parts[1];
            var value = parts[2];

            JsonElement fieldValue = default;
            if (variables.TryGetValue(field, out var varValue))
                fieldValue = varValue;
            else if (context.TryGetValue(field, out var ctxValue))
                fieldValue = ctxValue;

            var fieldStr = fieldValue.ValueKind != JsonValueKind.Undefined ? fieldValue.GetString() ?? string.Empty : string.Empty;

            return op switch
            {
                "==" => fieldStr == value,
                "!=" => fieldStr != value,
                "contains" => fieldStr.Contains(value),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static List<NodeDefinitionDto> ParseNodes(string nodesJson)
    {
        return JsonSerializer.Deserialize<List<NodeDefinitionDto>>(nodesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    private static List<TransitionDefinitionDto> ParseTransitions(string transitionsJson)
    {
        return JsonSerializer.Deserialize<List<TransitionDefinitionDto>>(transitionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    private static WorkflowNodeType ParseNodeType(string type)
    {
        return type switch
        {
            "Start" => WorkflowNodeType.Start,
            "Action" => WorkflowNodeType.Action,
            "Condition" => WorkflowNodeType.Condition,
            "Loop" => WorkflowNodeType.Loop,
            "Wait" => WorkflowNodeType.Wait,
            "Approval" => WorkflowNodeType.Approval,
            "Notification" => WorkflowNodeType.Notification,
            "End" => WorkflowNodeType.End,
            _ => WorkflowNodeType.Action
        };
    }

    private async Task<int> GetNextSequenceAsync(Guid instanceId, CancellationToken cancellationToken)
    {
        var steps = await _stepRepo.GetByInstanceAsync(instanceId, cancellationToken);
        return steps.Count + 1;
    }

    private static string ResolveExpression(string expression, WorkflowInstance? instance)
    {
        if (instance == null || !expression.Contains("{{")) return expression;

        var context = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(instance.ContextJson)
            ?? new Dictionary<string, JsonElement>();

        foreach (var kvp in context)
        {
            expression = expression.Replace($"{{{{{kvp.Key}}}}}", kvp.Value.GetString() ?? string.Empty);
        }

        return expression;
    }
}
