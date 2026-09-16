using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Security;

public class LostFoundService
{
    private readonly ILostFoundItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LostFoundService(
        ILostFoundItemRepository itemRepository,
        IUnitOfWork unitOfWork)
    {
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LostFoundItem> RegisterItemAsync(LostFoundItem item, CancellationToken cancellationToken = default)
    {
        item.Status = LostFoundStatus.Unclaimed;
        var created = await _itemRepository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task ClaimItemAsync(Guid itemId, Guid claimedByUserId, Guid handedOverByUserId, CancellationToken cancellationToken = default)
    {
        var item = await _itemRepository.GetByIdAsync(itemId, cancellationToken)
            ?? throw new KeyNotFoundException("Item not found.");

        item.Status = LostFoundStatus.Claimed;
        item.ClaimedByUserId = claimedByUserId;
        item.ClaimedAt = DateTime.UtcNow;
        item.HandedOverByUserId = handedOverByUserId;
        item.UpdatedAt = DateTime.UtcNow;

        await _itemRepository.UpdateAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
