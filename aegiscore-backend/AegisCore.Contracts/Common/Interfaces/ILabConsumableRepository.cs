using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface ILabConsumableRepository : IRepository<LabConsumable>
{
    Task<IReadOnlyList<LabConsumable>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabConsumable>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabConsumable>> GetExpiringAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
}
