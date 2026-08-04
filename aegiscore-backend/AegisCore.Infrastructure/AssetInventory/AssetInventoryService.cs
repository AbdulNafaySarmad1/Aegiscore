using System.Xml.Linq;
using AegisCore.Application.AssetInventory;
using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.AssetInventory;

public class AssetInventoryService : IAssetInventoryService
{
    private readonly AegisDbContext _db;
    public AssetInventoryService(AegisDbContext db) => _db = db;

    public decimal CalculateStraightLineDepreciation(decimal purchaseCost, int usefulLifeYears, DateTime purchaseDate, DateTime asAtDate)
    {
        if (purchaseCost < 0) throw new ArgumentOutOfRangeException(nameof(purchaseCost));
        if (usefulLifeYears <= 0) throw new ArgumentOutOfRangeException(nameof(usefulLifeYears));
        if (asAtDate <= purchaseDate) return 0m;
        var months = ((asAtDate.Year - purchaseDate.Year) * 12) + asAtDate.Month - purchaseDate.Month;
        var depreciation = purchaseCost / (usefulLifeYears * 12m) * months;
        return Math.Min(purchaseCost, Math.Round(depreciation, 2));
    }

    public async Task<decimal> CalculateCurrentValueAsync(Guid assetId, DateTime asAtDate, CancellationToken ct = default)
    {
        var asset = await _db.Assets.AsNoTracking().FirstAsync(a => a.Id == assetId, ct);
        var depreciation = CalculateStraightLineDepreciation(asset.PurchaseCost, asset.UsefulLifeYears, asset.PurchaseDate ?? asset.CreatedAt, asAtDate);
        return Math.Max(0m, asset.PurchaseCost - depreciation);
    }

    public async Task<IReadOnlyList<LabConsumable>> GetLowStockAsync(CancellationToken ct = default)
        => await _db.LabConsumables.AsNoTracking().Where(i => i.QuantityInStock <= i.MinimumStockLevel).OrderBy(i => i.ItemName).ToListAsync(ct);

    public async Task<string> BuildHemisXmlAsync(int academicYear, CancellationToken ct = default)
    {
        if (academicYear is < 2000 or > 2100) throw new ArgumentOutOfRangeException(nameof(academicYear));
        var assetCount = await _db.Assets.CountAsync(ct);
        var lowStockCount = await _db.LabConsumables.CountAsync(i => i.QuantityInStock <= i.MinimumStockLevel, ct);
        var evidenceCount = await _db.AccreditationEvidences.CountAsync(ct);
        var doc = new XDocument(new XElement("hemis",
            new XAttribute("academicYear", academicYear),
            new XAttribute("generatedAt", DateTime.UtcNow.ToString("O")),
            new XElement("assets", new XAttribute("count", assetCount)),
            new XElement("inventory", new XAttribute("lowStockCount", lowStockCount)),
            new XElement("accreditationEvidence", new XAttribute("count", evidenceCount))));
        return doc.ToString(SaveOptions.DisableFormatting);
    }
}
