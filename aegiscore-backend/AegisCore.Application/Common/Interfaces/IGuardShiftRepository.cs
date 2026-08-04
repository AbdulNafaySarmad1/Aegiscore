using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IGuardShiftRepository : IRepository<GuardShift>
{
    Task<IReadOnlyList<GuardShift>> GetByGuardAsync(Guid guardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuardShift>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuardShift>> GetByGateAsync(string gate, DateTime date, CancellationToken cancellationToken = default);
}
