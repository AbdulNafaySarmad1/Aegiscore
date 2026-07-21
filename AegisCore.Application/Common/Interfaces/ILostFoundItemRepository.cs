using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface ILostFoundItemRepository : IRepository<LostFoundItem>
{
    Task<IReadOnlyList<LostFoundItem>> GetByStatusAsync(LostFoundStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LostFoundItem>> GetByTypeAsync(LostFoundType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LostFoundItem>> GetUnclaimedAsync(CancellationToken cancellationToken = default);
}
