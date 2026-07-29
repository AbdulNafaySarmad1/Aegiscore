using AegisCore.Domain.Entities;

namespace AegisCore.Application.AssetInventory;

public interface IAssetInventoryService
{
    decimal CalculateStraightLineDepreciation(decimal purchaseCost, int usefulLifeYears, DateTime purchaseDate, DateTime asAtDate);
    Task<decimal> CalculateCurrentValueAsync(Guid assetId, DateTime asAtDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabConsumable>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<string> BuildHemisXmlAsync(int academicYear, CancellationToken cancellationToken = default);
}
