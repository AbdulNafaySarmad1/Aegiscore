using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IGuardRoundRepository : IRepository<GuardRound>
{
    Task<IReadOnlyList<GuardRound>> GetByGuardAsync(Guid guardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuardRound>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<GuardRound?> GetWithScansAsync(Guid roundId, CancellationToken cancellationToken = default);
}
