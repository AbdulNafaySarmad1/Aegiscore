using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IResearchLabRepository : IRepository<ResearchLab>
{
    Task<IReadOnlyList<ResearchLab>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResearchLab>> GetBySupervisorAsync(Guid supervisorId, CancellationToken cancellationToken = default);
    Task<ResearchLab?> GetWithEquipmentAsync(Guid labId, CancellationToken cancellationToken = default);
}
