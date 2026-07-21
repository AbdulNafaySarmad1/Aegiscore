using AegisCore.Domain.Entities;

namespace AegisCore.Application.Workflow;

public interface IWorkflowEngine
{
    Task<WorkflowInstance> StartWorkflowAsync(Guid definitionId, Guid triggeredByUserId, Dictionary<string, object?>? context = null, CancellationToken cancellationToken = default);
    Task AdvanceWorkflowAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task CancelWorkflowAsync(Guid instanceId, string reason, CancellationToken cancellationToken = default);
    Task SuspendWorkflowAsync(Guid instanceId, string reason, CancellationToken cancellationToken = default);
    Task ResumeWorkflowAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task RetryFailedStepAsync(Guid stepId, CancellationToken cancellationToken = default);
}

public interface IApprovalService
{
    Task<ApprovalRecord> ApproveAsync(Guid approvalId, Guid userId, string? comment, CancellationToken cancellationToken = default);
    Task<ApprovalRecord> RejectAsync(Guid approvalId, Guid userId, string? comment, CancellationToken cancellationToken = default);
    Task<ApprovalRecord> DelegateAsync(Guid approvalId, Guid fromUserId, Guid toUserId, string reason, CancellationToken cancellationToken = default);
    Task ProcessTimeoutAsync(Guid approvalId, CancellationToken cancellationToken = default);
    Task InitializeApprovalsForStep(Guid stepId, List<ApproverConfig> approvers, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
    Task SendInAppAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
    Task<NotificationRecord> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default);
}
