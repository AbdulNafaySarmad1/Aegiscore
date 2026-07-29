using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Workflow;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Workflow;

public class ApprovalService : IApprovalService
{
    private readonly IApprovalRecordRepository _approvalRepo;
    private readonly IWorkflowStepRepository _stepRepo;
    private readonly WorkflowEngine _workflowEngine;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        IApprovalRecordRepository approvalRepo,
        IWorkflowStepRepository stepRepo,
        WorkflowEngine workflowEngine,
        ILogger<ApprovalService> logger)
    {
        _approvalRepo = approvalRepo;
        _stepRepo = stepRepo;
        _workflowEngine = workflowEngine;
        _logger = logger;
    }

    public async Task InitializeApprovalsForStep(Guid stepId, List<ApproverConfig> configs, CancellationToken cancellationToken = default)
    {
        var step = await _stepRepo.GetByIdAsync(stepId, cancellationToken)
            ?? throw new InvalidOperationException("Step not found.");

        var approvers = configs;
        for (var i = 0; i < approvers.Count; i++)
        {
            var approver = approvers[i];
            var record = new ApprovalRecord
            {
                Id = Guid.NewGuid(),
                StepId = stepId,
                ApproverUserId = approver.UserId,
                ApproverRole = approver.Role,
                Sequence = approver.Sequence > 0 ? approver.Sequence : i + 1,
                Status = ApprovalStatus.Pending,
                TimeoutAt = DateTime.UtcNow.AddHours(48)
            };

            await _approvalRepo.AddAsync(record, cancellationToken);
        }

        _logger.LogInformation("Initialized {Count} approvals for step {StepId}", approvers.Count, stepId);
    }

    public async Task<ApprovalRecord> ApproveAsync(Guid approvalId, Guid userId, string? comment, CancellationToken cancellationToken = default)
    {
        var record = await _approvalRepo.GetByIdAsync(approvalId, cancellationToken)
            ?? throw new InvalidOperationException("Approval record not found.");

        if (record.ApproverUserId != userId && record.DelegatedToUserId != userId)
            throw new UnauthorizedAccessException("You are not authorized to approve this record.");

        if (record.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Approval is not pending. Current status: {record.Status}");

        record.Status = ApprovalStatus.Approved;
        record.Comment = comment;
        record.DecidedAt = DateTime.UtcNow;
        await _approvalRepo.UpdateAsync(record, cancellationToken);

        var allRecords = await _approvalRepo.GetByStepAsync(record.StepId, cancellationToken);
        var allApproved = allRecords.All(a => a.Status == ApprovalStatus.Approved);

        await _workflowEngine.OnApprovalDecidedAsync(record.StepId, allApproved, cancellationToken);

        return record;
    }

    public async Task<ApprovalRecord> RejectAsync(Guid approvalId, Guid userId, string? comment, CancellationToken cancellationToken = default)
    {
        var record = await _approvalRepo.GetByIdAsync(approvalId, cancellationToken)
            ?? throw new InvalidOperationException("Approval record not found.");

        if (record.ApproverUserId != userId && record.DelegatedToUserId != userId)
            throw new UnauthorizedAccessException("You are not authorized to reject this record.");

        if (record.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Approval is not pending. Current status: {record.Status}");

        record.Status = ApprovalStatus.Rejected;
        record.Comment = comment;
        record.DecidedAt = DateTime.UtcNow;
        await _approvalRepo.UpdateAsync(record, cancellationToken);

        await _workflowEngine.OnApprovalDecidedAsync(record.StepId, false, cancellationToken);

        return record;
    }

    public async Task<ApprovalRecord> DelegateAsync(Guid approvalId, Guid fromUserId, Guid toUserId, string reason, CancellationToken cancellationToken = default)
    {
        var record = await _approvalRepo.GetByIdAsync(approvalId, cancellationToken)
            ?? throw new InvalidOperationException("Approval record not found.");

        if (record.ApproverUserId != fromUserId)
            throw new UnauthorizedAccessException("You are not the assigned approver for this record.");

        if (record.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException("Can only delegate pending approvals.");

        record.Status = ApprovalStatus.Delegated;
        record.DelegatedToUserId = toUserId;
        record.DelegationReason = reason;
        await _approvalRepo.UpdateAsync(record, cancellationToken);

        var delegatedRecord = new ApprovalRecord
        {
            Id = Guid.NewGuid(),
            StepId = record.StepId,
            ApproverUserId = toUserId,
            ApproverRole = record.ApproverRole,
            Sequence = record.Sequence,
            Status = ApprovalStatus.Pending,
            TimeoutAt = record.TimeoutAt
        };

        await _approvalRepo.AddAsync(delegatedRecord, cancellationToken);

        _logger.LogInformation("Approval {ApprovalId} delegated from {FromUser} to {ToUser}: {Reason}", approvalId, fromUserId, toUserId, reason);

        return delegatedRecord;
    }

    public async Task ProcessTimeoutAsync(Guid approvalId, CancellationToken cancellationToken = default)
    {
        var record = await _approvalRepo.GetByIdAsync(approvalId, cancellationToken)
            ?? throw new InvalidOperationException("Approval record not found.");

        if (record.Status != ApprovalStatus.Pending)
            return;

        record.Status = ApprovalStatus.TimedOut;
        record.IsEscalated = true;
        await _approvalRepo.UpdateAsync(record, cancellationToken);

        _logger.LogWarning("Approval {ApprovalId} timed out and was escalated.", approvalId);
    }
}
