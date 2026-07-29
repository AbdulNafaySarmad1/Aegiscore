using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class ComplianceFrameworkRepository : Repository<ComplianceFramework>, IComplianceFrameworkRepository
{
    public ComplianceFrameworkRepository(AegisDbContext context) : base(context) { }

    public async Task<ComplianceFramework?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(f => f.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<ComplianceFramework>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(f => f.Status == ComplianceFrameworkStatus.Active).ToListAsync(cancellationToken);
    }

    public async Task<ComplianceFramework?> GetWithControlsAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(f => f.Controls).FirstOrDefaultAsync(f => f.Id == frameworkId, cancellationToken);
    }
}

public class ComplianceControlRepository : Repository<ComplianceControl>, IComplianceControlRepository
{
    public ComplianceControlRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ComplianceControl>> GetByFrameworkAsync(Guid frameworkId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.FrameworkId == frameworkId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ComplianceControl>> GetByStatusAsync(ControlStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ComplianceControl>> GetDueForReviewAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.NextReviewDate.HasValue && c.NextReviewDate.Value <= before).ToListAsync(cancellationToken);
    }

    public async Task<ComplianceControl?> GetWithEvidencesAsync(Guid controlId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(c => c.Evidences).FirstOrDefaultAsync(c => c.Id == controlId, cancellationToken);
    }
}

public class AuditFindingRepository : Repository<AuditFinding>, IAuditFindingRepository
{
    public AuditFindingRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AuditFinding>> GetByControlAsync(Guid controlId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(f => f.ControlId == controlId).OrderByDescending(f => f.IdentifiedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditFinding>> GetByStatusAsync(AuditFindingStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(f => f.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditFinding>> GetBySeverityAsync(AuditFindingSeverity severity, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(f => f.Severity == severity).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditFinding>> GetOverdueAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(f =>
            f.TargetResolutionDate.HasValue && f.TargetResolutionDate.Value <= now &&
            (f.Status == AuditFindingStatus.Open || f.Status == AuditFindingStatus.InProgress)
        ).ToListAsync(cancellationToken);
    }

    public async Task<AuditFinding?> GetWithActionPlansAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(f => f.ActionPlans).FirstOrDefaultAsync(f => f.Id == findingId, cancellationToken);
    }
}

public class CorrectiveActionPlanRepository : Repository<CorrectiveActionPlan>, ICorrectiveActionPlanRepository
{
    public CorrectiveActionPlanRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<CorrectiveActionPlan>> GetByFindingAsync(Guid findingId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.FindingId == findingId).OrderBy(c => c.DueDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CorrectiveActionPlan>> GetByAssigneeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.AssignedToUserId == userId && c.Status != CAPStatus.Completed).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CorrectiveActionPlan>> GetOverdueAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.DueDate <= now && (c.Status == CAPStatus.Planned || c.Status == CAPStatus.InProgress)).ToListAsync(cancellationToken);
    }
}
