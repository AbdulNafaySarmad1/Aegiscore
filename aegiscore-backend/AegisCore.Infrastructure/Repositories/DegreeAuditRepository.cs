using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class DegreeProgramRepository : Repository<DegreeProgram>, IDegreeProgramRepository
{
    public DegreeProgramRepository(AegisDbContext context) : base(context) { }

    public async Task<DegreeProgram?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<DegreeProgram>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.DepartmentId == departmentId).ToListAsync(cancellationToken);
    }

    public async Task<DegreeProgram?> GetWithRequirementsAsync(Guid programId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(p => p.Requirements).FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
    }
}

public class DegreeRequirementRepository : Repository<DegreeRequirement>, IDegreeRequirementRepository
{
    public DegreeRequirementRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<DegreeRequirement>> GetByProgramAsync(Guid programId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.ProgramId == programId).OrderBy(r => r.SortOrder).ToListAsync(cancellationToken);
    }
}

public class DegreeAuditRepository : Repository<DegreeAuditEntity>, IDegreeAuditRepository
{
    public DegreeAuditRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<DegreeAuditEntity>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(a => a.Requirement).Where(a => a.StudentId == studentId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DegreeAuditEntity>> GetByProgramAsync(Guid programId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(a => a.ProgramId == programId).ToListAsync(cancellationToken);
    }

    public async Task<DegreeAuditEntity?> GetByStudentAndRequirementAsync(Guid studentId, Guid requirementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.StudentId == studentId && a.RequirementId == requirementId, cancellationToken);
    }
}

public class CreditTransferRepository : Repository<CreditTransfer>, ICreditTransferRepository
{
    public CreditTransferRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<CreditTransfer>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.StudentId == studentId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CreditTransfer>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => !t.IsApproved).ToListAsync(cancellationToken);
    }
}
