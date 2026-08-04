using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface ILabSafetyInspectionRepository : IRepository<LabSafetyInspection>
{
    Task<IReadOnlyList<LabSafetyInspection>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabSafetyInspection>> GetByStatusAsync(InspectionStatus status, CancellationToken cancellationToken = default);
    Task<LabSafetyInspection?> GetWithChecklistAsync(Guid inspectionId, CancellationToken cancellationToken = default);
}
