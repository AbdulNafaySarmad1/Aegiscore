using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Workflow;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Workflow;

public class WorkflowTemplateSeeder
{
    private readonly IWorkflowDefinitionRepository _definitionRepo;
    private readonly ILogger<WorkflowTemplateSeeder> _logger;

    public WorkflowTemplateSeeder(
        IWorkflowDefinitionRepository definitionRepo,
        ILogger<WorkflowTemplateSeeder> logger)
    {
        _definitionRepo = definitionRepo;
        _logger = logger;
    }

    public async Task SeedAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var templates = new[]
        {
            CreateLeaveApprovalTemplate(tenantId),
            CreatePurchaseRequestTemplate(tenantId),
            CreateComplaintEscalationTemplate(tenantId),
            CreateGradeAppealTemplate(tenantId)
        };

        foreach (var template in templates)
        {
            var existing = await _definitionRepo.GetByNameAndVersionAsync(template.Name, 1, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("Template '{TemplateName}' already exists, skipping.", template.Name);
                continue;
            }

            await _definitionRepo.AddAsync(template, cancellationToken);
            _logger.LogInformation("Seeded workflow template: {TemplateName}", template.Name);
        }
    }

    private static WorkflowDefinition CreateLeaveApprovalTemplate(Guid tenantId)
    {
        var nodes = new List<NodeDefinitionDto>
        {
            new() { Id = "start", Label = "Leave Request Received", Type = "Start", X = 100, Y = 50 },
            new()
            {
                Id = "notify_hr", Label = "Notify HR Manager", Type = "Notification", X = 100, Y = 150,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{hrEmail}}",
                        SubjectTemplate = "New Leave Request from {{employeeName}}",
                        BodyTemplate = "<h2>Leave Request</h2><p><strong>Employee:</strong> {{employeeName}}</p><p><strong>Type:</strong> {{leaveType}}</p><p><strong>Dates:</strong> {{startDate}} to {{endDate}}</p><p>Please review and approve.</p>"
                    }
                }
            },
            new()
            {
                Id = "approval", Label = "Manager Approval", Type = "Approval", X = 100, Y = 250,
                Config = new NodeConfigDto
                {
                    Approval = new ApprovalConfigDto
                    {
                        Type = "Sequential",
                        TimeoutHours = 72,
                        RejectTransitionId = "notify_reject",
                        Approvers = new List<ApproverConfigDto>
                        {
                            new() { UserId = Guid.Empty, Role = "DepartmentHead", Sequence = 1 },
                            new() { UserId = Guid.Empty, Role = "HRManager", Sequence = 2 }
                        }
                    }
                }
            },
            new()
            {
                Id = "notify_approve", Label = "Notify Employee (Approved)", Type = "Notification", X = 300, Y = 350,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{employeeEmail}}",
                        SubjectTemplate = "Leave Request Approved",
                        BodyTemplate = "<h2>Your leave request has been approved.</h2><p><strong>Dates:</strong> {{startDate}} to {{endDate}}</p><p>Enjoy your time off!</p>"
                    }
                }
            },
            new()
            {
                Id = "notify_reject", Label = "Notify Employee (Rejected)", Type = "Notification", X = -100, Y = 350,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{employeeEmail}}",
                        SubjectTemplate = "Leave Request Rejected",
                        BodyTemplate = "<h2>Your leave request has been rejected.</h2><p>Please contact your manager for details.</p>"
                    }
                }
            },
            new() { Id = "end", Label = "Complete", Type = "End", X = 300, Y = 450 },
            new() { Id = "end_reject", Label = "Complete (Rejected)", Type = "End", X = -100, Y = 450 }
        };

        var transitions = new List<TransitionDefinitionDto>
        {
            new() { Id = "t1", SourceNodeId = "start", TargetNodeId = "notify_hr", Label = "Notify HR" },
            new() { Id = "t2", SourceNodeId = "notify_hr", TargetNodeId = "approval", Label = "Send for Approval" },
            new() { Id = "t3", SourceNodeId = "approval", TargetNodeId = "notify_approve", Label = "Approved" },
            new() { Id = "t4", SourceNodeId = "notify_approve", TargetNodeId = "end", Label = "Done" },
            new() { Id = "t5", SourceNodeId = "notify_reject", TargetNodeId = "end_reject", Label = "Done" }
        };

        return new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Leave Approval",
            Description = "Standard leave request workflow: notification → sequential approval (dept head → HR) → confirmation email.",
            Version = 1,
            Status = WorkflowDefinitionStatus.Published,
            TriggerType = WorkflowTriggerType.Manual,
            IsTemplate = true,
            Category = "HR",
            NodesJson = JsonSerializer.Serialize(nodes),
            TransitionsJson = JsonSerializer.Serialize(transitions),
            CreatedByUserId = Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
    }

    private static WorkflowDefinition CreatePurchaseRequestTemplate(Guid tenantId)
    {
        var nodes = new List<NodeDefinitionDto>
        {
            new() { Id = "start", Label = "Purchase Request", Type = "Start", X = 100, Y = 50 },
            new()
            {
                Id = "check_amount", Label = "Amount > $1000?", Type = "Condition", X = 100, Y = 150,
                Config = new NodeConfigDto
                {
                    Condition = new ConditionConfigDto
                    {
                        Field = "amount",
                        Operator = "GreaterThan",
                        Value = "1000"
                    }
                }
            },
            new()
            {
                Id = "approval_finance", Label = "Finance Director Approval", Type = "Approval", X = -100, Y = 250,
                Config = new NodeConfigDto
                {
                    Approval = new ApprovalConfigDto
                    {
                        Type = "Single",
                        TimeoutHours = 48,
                        RejectTransitionId = "notify_reject",
                        Approvers = new List<ApproverConfigDto>
                        {
                            new() { UserId = Guid.Empty, Role = "FinanceDirector" }
                        }
                    }
                }
            },
            new()
            {
                Id = "approval_manager", Label = "Manager Approval", Type = "Approval", X = 300, Y = 250,
                Config = new NodeConfigDto
                {
                    Approval = new ApprovalConfigDto
                    {
                        Type = "Single",
                        TimeoutHours = 24,
                        RejectTransitionId = "notify_reject",
                        Approvers = new List<ApproverConfigDto>
                        {
                            new() { UserId = Guid.Empty, Role = "DepartmentHead" }
                        }
                    }
                }
            },
            new()
            {
                Id = "notify_approve", Label = "Notify Requester", Type = "Notification", X = 100, Y = 350,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{requesterEmail}}",
                        SubjectTemplate = "Purchase Request Approved: {{itemName}}",
                        BodyTemplate = "Your purchase request for {{itemName}} (${{amount}}) has been approved."
                    }
                }
            },
            new()
            {
                Id = "notify_reject", Label = "Notify Rejection", Type = "Notification", X = 100, Y = 350,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{requesterEmail}}",
                        SubjectTemplate = "Purchase Request Rejected",
                        BodyTemplate = "Your purchase request for {{itemName}} has been rejected."
                    }
                }
            },
            new() { Id = "end", Label = "Complete", Type = "End", X = 100, Y = 450 }
        };

        var transitions = new List<TransitionDefinitionDto>
        {
            new() { Id = "t1", SourceNodeId = "start", TargetNodeId = "check_amount" },
            new() { Id = "t2", SourceNodeId = "check_amount", TargetNodeId = "approval_finance", Label = "Yes", ConditionExpression = "amount > 1000" },
            new() { Id = "t3", SourceNodeId = "check_amount", TargetNodeId = "approval_manager", Label = "No", ConditionExpression = "amount <= 1000" },
            new() { Id = "t4", SourceNodeId = "approval_finance", TargetNodeId = "notify_approve", Label = "Approved" },
            new() { Id = "t5", SourceNodeId = "approval_manager", TargetNodeId = "notify_approve", Label = "Approved" }
        };

        return new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Purchase Request",
            Description = "Conditional purchase request: over $1,000 requires Finance Director approval.",
            Version = 1,
            Status = WorkflowDefinitionStatus.Published,
            TriggerType = WorkflowTriggerType.Manual,
            IsTemplate = true,
            Category = "Finance",
            NodesJson = JsonSerializer.Serialize(nodes),
            TransitionsJson = JsonSerializer.Serialize(transitions),
            CreatedByUserId = Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
    }

    private static WorkflowDefinition CreateComplaintEscalationTemplate(Guid tenantId)
    {
        var nodes = new List<NodeDefinitionDto>
        {
            new() { Id = "start", Label = "Complaint Filed", Type = "Start", X = 100, Y = 50 },
            new()
            {
                Id = "notify_dept", Label = "Notify Dept Head", Type = "Notification", X = 100, Y = 150,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{deptHeadEmail}}",
                        SubjectTemplate = "New Complaint: {{complaintType}}",
                        BodyTemplate = "<h2>Complaint Received</h2><p><strong>Type:</strong> {{complaintType}}</p><p><strong>Filed by:</strong> {{filerName}}</p><p>{{description}}</p>"
                    }
                }
            },
            new()
            {
                Id = "approval", Label = "Dept Head Review", Type = "Approval", X = 100, Y = 250,
                Config = new NodeConfigDto
                {
                    Approval = new ApprovalConfigDto
                    {
                        Type = "Single",
                        TimeoutHours = 24,
                        EscalationUserId = "dean",
                        RejectTransitionId = "notify_close",
                        Approvers = new List<ApproverConfigDto>
                        {
                            new() { UserId = Guid.Empty, Role = "DepartmentHead" }
                        }
                    }
                }
            },
            new()
            {
                Id = "notify_resolve", Label = "Notify Resolution", Type = "Notification", X = 100, Y = 350,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{filerEmail}}",
                        SubjectTemplate = "Complaint Resolved: {{complaintType}}",
                        BodyTemplate = "Your complaint regarding {{complaintType}} has been resolved."
                    }
                }
            },
            new()
            {
                Id = "notify_close", Label = "Notify Closure", Type = "Notification", X = -100, Y = 350,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{filerEmail}}",
                        SubjectTemplate = "Complaint Closed: {{complaintType}}",
                        BodyTemplate = "Your complaint has been reviewed and closed."
                    }
                }
            },
            new() { Id = "end", Label = "Complete", Type = "End", X = 100, Y = 450 }
        };

        var transitions = new List<TransitionDefinitionDto>
        {
            new() { Id = "t1", SourceNodeId = "start", TargetNodeId = "notify_dept" },
            new() { Id = "t2", SourceNodeId = "notify_dept", TargetNodeId = "approval" },
            new() { Id = "t3", SourceNodeId = "approval", TargetNodeId = "notify_resolve", Label = "Resolved" }
        };

        return new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Complaint Escalation",
            Version = 1,
            Status = WorkflowDefinitionStatus.Published,
            TriggerType = WorkflowTriggerType.Manual,
            IsTemplate = true,
            Category = "Operations",
            NodesJson = JsonSerializer.Serialize(nodes),
            TransitionsJson = JsonSerializer.Serialize(transitions),
            CreatedByUserId = Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
    }

    private static WorkflowDefinition CreateGradeAppealTemplate(Guid tenantId)
    {
        var nodes = new List<NodeDefinitionDto>
        {
            new() { Id = "start", Label = "Grade Appeal Filed", Type = "Start", X = 100, Y = 50 },
            new()
            {
                Id = "notify_faculty", Label = "Notify Faculty", Type = "Notification", X = 100, Y = 150,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{facultyEmail}}",
                        SubjectTemplate = "Grade Appeal: {{courseCode}} - {{studentName}}",
                        BodyTemplate = "<h2>Grade Appeal</h2><p><strong>Student:</strong> {{studentName}}</p><p><strong>Course:</strong> {{courseCode}}</p><p><strong>Current Grade:</strong> {{currentGrade}}</p><p><strong>Reason:</strong> {{appealReason}}</p>"
                    }
                }
            },
            new()
            {
                Id = "approval", Label = "Faculty Decision", Type = "Approval", X = 100, Y = 250,
                Config = new NodeConfigDto
                {
                    Approval = new ApprovalConfigDto
                    {
                        Type = "Single",
                        TimeoutHours = 120,
                        RejectTransitionId = "notify_deny",
                        Approvers = new List<ApproverConfigDto>
                        {
                            new() { UserId = Guid.Empty, Role = "Faculty" }
                        }
                    }
                }
            },
            new()
            {
                Id = "notify_approve", Label = "Notify Student (Approved)", Type = "Notification", X = 300, Y = 350,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{studentEmail}}",
                        SubjectTemplate = "Grade Appeal Approved: {{courseCode}}",
                        BodyTemplate = "Your grade appeal for {{courseCode}} has been approved. Your grade has been updated."
                    }
                }
            },
            new()
            {
                Id = "notify_deny", Label = "Notify Student (Denied)", Type = "Notification", X = -100, Y = 350,
                Config = new NodeConfigDto
                {
                    Notification = new NotificationConfigDto
                    {
                        Channel = "Email",
                        RecipientExpression = "{{studentEmail}}",
                        SubjectTemplate = "Grade Appeal Denied: {{courseCode}}",
                        BodyTemplate = "Your grade appeal for {{courseCode}} has been reviewed and the original grade stands."
                    }
                }
            },
            new() { Id = "end", Label = "Complete", Type = "End", X = 300, Y = 450 }
        };

        var transitions = new List<TransitionDefinitionDto>
        {
            new() { Id = "t1", SourceNodeId = "start", TargetNodeId = "notify_faculty" },
            new() { Id = "t2", SourceNodeId = "notify_faculty", TargetNodeId = "approval" },
            new() { Id = "t3", SourceNodeId = "approval", TargetNodeId = "notify_approve", Label = "Approved" }
        };

        return new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Grade Appeal",
            Version = 1,
            Status = WorkflowDefinitionStatus.Published,
            TriggerType = WorkflowTriggerType.Manual,
            IsTemplate = true,
            Category = "Academic",
            NodesJson = JsonSerializer.Serialize(nodes),
            TransitionsJson = JsonSerializer.Serialize(transitions),
            CreatedByUserId = Guid.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
    }
}
