using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IEquipmentMaintenanceRepository : IRepository<EquipmentMaintenance>
{
    Task<IReadOnlyList<EquipmentMaintenance>> GetByEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EquipmentMaintenance>> GetByStatusAsync(MaintenanceStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EquipmentMaintenance>> GetPendingAsync(CancellationToken cancellationToken = default);
}
