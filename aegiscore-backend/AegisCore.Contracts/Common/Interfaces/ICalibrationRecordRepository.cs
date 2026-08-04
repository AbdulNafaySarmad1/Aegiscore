using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface ICalibrationRecordRepository : IRepository<CalibrationRecord>
{
    Task<IReadOnlyList<CalibrationRecord>> GetByEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalibrationRecord>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<CalibrationRecord?> GetLatestAsync(Guid equipmentId, CancellationToken cancellationToken = default);
}
