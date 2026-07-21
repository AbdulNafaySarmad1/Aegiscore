using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class WorkflowDefinitionRepository : Repository<WorkflowDefinition>, IWorkflowDefinitionRepository
{
    public WorkflowDefinitionRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<WorkflowDefinition>> GetByStatusAsync(WorkflowDefinitionStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(d => d.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<WorkflowDefinition?> GetByNameAndVersionAsync(string name, int version, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.Name == name && d.Version == version, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(d => d.IsTemplate).ToListAsync(cancellationToken);
    }

    public async Task<WorkflowDefinition?> GetLatestPublishedAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.Name == name && d.Status == WorkflowDefinitionStatus.Published)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public class WorkflowInstanceRepository : Repository<WorkflowInstance>, IWorkflowInstanceRepository
{
    public WorkflowInstanceRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<WorkflowInstance>> GetByDefinitionAsync(Guid definitionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.DefinitionId == definitionId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.Status == WorkflowInstanceStatus.Running || i.Status == WorkflowInstanceStatus.Suspended).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowInstance>> GetByStatusAsync(WorkflowInstanceStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<WorkflowInstance?> GetWithStepsAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Steps)
                .ThenInclude(s => s.ApprovalRecords)
            .Include(i => i.Notifications)
            .FirstOrDefaultAsync(i => i.Id == instanceId, cancellationToken);
    }
}

public class WorkflowStepRepository : Repository<WorkflowStep>, IWorkflowStepRepository
{
    public WorkflowStepRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<WorkflowStep>> GetByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.InstanceId == instanceId)
            .Include(s => s.ApprovalRecords)
            .OrderBy(s => s.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowStep?> GetByInstanceAndNodeAsync(Guid instanceId, string nodeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.ApprovalRecords)
            .FirstOrDefaultAsync(s => s.InstanceId == instanceId && s.NodeId == nodeId, cancellationToken);
    }
}

public class ApprovalRecordRepository : Repository<ApprovalRecord>, IApprovalRecordRepository
{
    public ApprovalRecordRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ApprovalRecord>> GetByStepAsync(Guid stepId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(a => a.StepId == stepId).OrderBy(a => a.Sequence).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalRecord>> GetPendingByApproverAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(a => a.ApproverUserId == userId && a.Status == ApprovalStatus.Pending).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalRecord>> GetTimedOutAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(a => a.Status == ApprovalStatus.Pending && a.TimeoutAt < DateTime.UtcNow).ToListAsync(cancellationToken);
    }
}

public class NotificationRecordRepository : Repository<NotificationRecord>, INotificationRecordRepository
{
    public NotificationRecordRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<NotificationRecord>> GetByInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(n => n.InstanceId == instanceId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationRecord>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(n => n.Status == NotificationStatus.Pending).ToListAsync(cancellationToken);
    }
}
