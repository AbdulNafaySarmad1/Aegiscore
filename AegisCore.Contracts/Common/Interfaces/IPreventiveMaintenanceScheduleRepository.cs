using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IPreventiveMaintenanceScheduleRepository : IRepository<PreventiveMaintenanceSchedule>
{
    Task<IReadOnlyList<PreventiveMaintenanceSchedule>> GetByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PreventiveMaintenanceSchedule>> GetDueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PreventiveMaintenanceSchedule>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PreventiveMaintenanceSchedule>> GetActiveAsync(CancellationToken cancellationToken = default);
}
