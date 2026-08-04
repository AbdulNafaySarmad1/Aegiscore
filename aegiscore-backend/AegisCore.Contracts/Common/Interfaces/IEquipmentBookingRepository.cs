using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IEquipmentBookingRepository : IRepository<EquipmentBooking>
{
    Task<IReadOnlyList<EquipmentBooking>> GetByEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EquipmentBooking>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasConflictAsync(Guid equipmentId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}
