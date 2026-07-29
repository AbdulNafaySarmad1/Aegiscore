using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IMaintenanceContractorRepository : IRepository<MaintenanceContractor>
{
    Task<IReadOnlyList<MaintenanceContractor>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceContractor>> GetBySpecializationAsync(string specialization, CancellationToken cancellationToken = default);
}
