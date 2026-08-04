using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface ILabEquipmentRepository : IRepository<LabEquipment>
{
    Task<IReadOnlyList<LabEquipment>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabEquipment>> GetByStatusAsync(EquipmentStatus status, CancellationToken cancellationToken = default);
    Task<LabEquipment?> GetByAssetTagAsync(string assetTag, CancellationToken cancellationToken = default);
}
