using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IGateEntryRepository : IRepository<GateEntry>
{
    Task<IReadOnlyList<GateEntry>> GetByVehiclePassAsync(Guid vehiclePassId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GateEntry>> GetByVisitorAsync(Guid visitorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GateEntry>> GetByGateAsync(string gateName, DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GateEntry>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
