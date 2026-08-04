using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IMaintenanceContractorRepository : IRepository<MaintenanceContractor>
{
    Task<IReadOnlyList<MaintenanceContractor>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceContractor>> GetBySpecializationAsync(string specialization, CancellationToken cancellationToken = default);
}
