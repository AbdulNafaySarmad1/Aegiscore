using AegisCore.Domain.Entities;
using DegreeAuditEntity = AegisCore.Domain.Entities.DegreeAudit;

namespace AegisCore.Application.Common.Interfaces;

public interface IDegreeProgramRepository : IRepository<DegreeProgram>
{
    Task<DegreeProgram?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DegreeProgram>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<DegreeProgram?> GetWithRequirementsAsync(Guid programId, CancellationToken cancellationToken = default);
}

public interface IDegreeRequirementRepository : IRepository<DegreeRequirement>
{
    Task<IReadOnlyList<DegreeRequirement>> GetByProgramAsync(Guid programId, CancellationToken cancellationToken = default);
}

public interface IDegreeAuditRepository : IRepository<DegreeAuditEntity>
{
    Task<IReadOnlyList<DegreeAuditEntity>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DegreeAuditEntity>> GetByProgramAsync(Guid programId, CancellationToken cancellationToken = default);
    Task<DegreeAuditEntity?> GetByStudentAndRequirementAsync(Guid studentId, Guid requirementId, CancellationToken cancellationToken = default);
}

public interface ICreditTransferRepository : IRepository<CreditTransfer>
{
    Task<IReadOnlyList<CreditTransfer>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CreditTransfer>> GetPendingAsync(CancellationToken cancellationToken = default);
}
