using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Workflow;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Workflow;

public static class WorkflowServiceExtensions
{
    public static IServiceCollection AddWorkflowStudio(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IWorkflowDefinitionRepository, WorkflowDefinitionRepository>();
        services.TryAddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
        services.TryAddScoped<IWorkflowStepRepository, WorkflowStepRepository>();
        services.TryAddScoped<IApprovalRecordRepository, ApprovalRecordRepository>();
        services.TryAddScoped<INotificationRecordRepository, NotificationRecordRepository>();

        services.TryAddScoped<IApprovalService, ApprovalService>();
        services.TryAddScoped<INotificationService, EmailNotificationService>();
        services.TryAddScoped<WorkflowEngine>();
        services.TryAddScoped<IWorkflowEngine>(sp => sp.GetRequiredService<WorkflowEngine>());

        services.TryAddTransient<WorkflowTemplateSeeder>();

        return services;
    }
}
