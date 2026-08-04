using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

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

public interface IDegreeAuditRepository : IRepository<DegreeAudit>
{
    Task<IReadOnlyList<DegreeAudit>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DegreeAudit>> GetByProgramAsync(Guid programId, CancellationToken cancellationToken = default);
    Task<DegreeAudit?> GetByStudentAndRequirementAsync(Guid studentId, Guid requirementId, CancellationToken cancellationToken = default);
}

public interface ICreditTransferRepository : IRepository<CreditTransfer>
{
    Task<IReadOnlyList<CreditTransfer>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CreditTransfer>> GetPendingAsync(CancellationToken cancellationToken = default);
}
