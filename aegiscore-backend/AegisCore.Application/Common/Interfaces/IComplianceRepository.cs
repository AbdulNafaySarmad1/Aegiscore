using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IComplianceFrameworkRepository : IRepository<ComplianceFramework>
{
    Task<ComplianceFramework?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceFramework>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<ComplianceFramework?> GetWithControlsAsync(Guid frameworkId, CancellationToken cancellationToken = default);
}

public interface IComplianceControlRepository : IRepository<ComplianceControl>
{
    Task<IReadOnlyList<ComplianceControl>> GetByFrameworkAsync(Guid frameworkId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceControl>> GetByStatusAsync(ControlStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ComplianceControl>> GetDueForReviewAsync(DateTime before, CancellationToken cancellationToken = default);
    Task<ComplianceControl?> GetWithEvidencesAsync(Guid controlId, CancellationToken cancellationToken = default);
}

public interface IAuditFindingRepository : IRepository<AuditFinding>
{
    Task<IReadOnlyList<AuditFinding>> GetByControlAsync(Guid controlId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditFinding>> GetByStatusAsync(AuditFindingStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditFinding>> GetBySeverityAsync(AuditFindingSeverity severity, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditFinding>> GetOverdueAsync(DateTime now, CancellationToken cancellationToken = default);
    Task<AuditFinding?> GetWithActionPlansAsync(Guid findingId, CancellationToken cancellationToken = default);
}

public interface ICorrectiveActionPlanRepository : IRepository<CorrectiveActionPlan>
{
    Task<IReadOnlyList<CorrectiveActionPlan>> GetByFindingAsync(Guid findingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CorrectiveActionPlan>> GetByAssigneeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CorrectiveActionPlan>> GetOverdueAsync(DateTime now, CancellationToken cancellationToken = default);
}
