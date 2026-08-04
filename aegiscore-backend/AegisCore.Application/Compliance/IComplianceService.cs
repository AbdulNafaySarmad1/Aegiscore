using AegisCore.Domain.Entities;

namespace AegisCore.Application.Compliance;

public interface IComplianceService
{
    Task<ComplianceFramework> RegisterFrameworkAsync(string name, string code, string description, CancellationToken cancellationToken = default);
    Task<ComplianceControl> AddControlAsync(Guid frameworkId, string controlCode, string title, string description, Guid ownerUserId, string ownerName, CancellationToken cancellationToken = default);
    Task<ComplianceControl> UpdateControlStatusAsync(Guid controlId, ControlStatus newStatus, string? notes, CancellationToken cancellationToken = default);
    Task<ControlEvidence> AttachEvidenceAsync(Guid controlId, string fileUrl, string description, Guid uploadedByUserId, CancellationToken cancellationToken = default);
    Task<AuditFinding> ReportFindingAsync(Guid controlId, string title, string description, AuditFindingSeverity severity, Guid identifiedById, string identifiedByName, CancellationToken cancellationToken = default);
    Task<CorrectiveActionPlan> CreateActionPlanAsync(Guid findingId, string description, Guid assigneeId, string assigneeName, DateTime dueDate, CancellationToken cancellationToken = default);
    Task<CorrectiveActionPlan> CompleteActionPlanAsync(Guid capId, string completionNotes, CancellationToken cancellationToken = default);
    Task<ComplianceDashboardDto> GetDashboardAsync(Guid? frameworkId, CancellationToken cancellationToken = default);
}
