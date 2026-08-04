using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IWorkflowDefinitionRepository : IRepository<WorkflowDefinition>
{
    Task<IReadOnlyList<WorkflowDefinition>> GetByStatusAsync(WorkflowDefinitionStatus status, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> GetByNameAndVersionAsync(string name, int version, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowDefinition>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> GetLatestPublishedAsync(string name, CancellationToken cancellationToken = default);
}

public interface IWorkflowInstanceRepository : IRepository<WorkflowInstance>
{
    Task<IReadOnlyList<WorkflowInstance>> GetByDefinitionAsync(Guid definitionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowInstance>> GetByStatusAsync(WorkflowInstanceStatus status, CancellationToken cancellationToken = default);
    Task<WorkflowInstance?> GetWithStepsAsync(Guid instanceId, CancellationToken cancellationToken = default);
}

public interface IWorkflowStepRepository : IRepository<WorkflowStep>
{
    Task<IReadOnlyList<WorkflowStep>> GetByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<WorkflowStep?> GetByInstanceAndNodeAsync(Guid instanceId, string nodeId, CancellationToken cancellationToken = default);
}

public interface IApprovalRecordRepository : IRepository<ApprovalRecord>
{
    Task<IReadOnlyList<ApprovalRecord>> GetByStepAsync(Guid stepId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalRecord>> GetPendingByApproverAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalRecord>> GetTimedOutAsync(CancellationToken cancellationToken = default);
}

public interface INotificationRecordRepository : IRepository<NotificationRecord>
{
    Task<IReadOnlyList<NotificationRecord>> GetByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationRecord>> GetPendingAsync(CancellationToken cancellationToken = default);
}
