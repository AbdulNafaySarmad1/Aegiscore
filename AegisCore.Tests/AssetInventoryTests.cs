using AegisCore.Infrastructure.AssetInventory;

namespace AegisCore.Tests;

public sealed class AssetInventoryTests
{
    [Fact]
    public void Straight_Line_Depreciation_Is_Capped_At_Cost()
    {
        var service = new AssetInventoryService(null!);
        var depreciation = service.CalculateStraightLineDepreciation(120000m, 5, new DateTime(2020, 1, 1), new DateTime(2030, 1, 1));
        Assert.Equal(120000m, depreciation);
    }

    [Fact]
    public void Straight_Line_Depreciation_Uses_Completed_Months()
    {
        var service = new AssetInventoryService(null!);
        var depreciation = service.CalculateStraightLineDepreciation(120000m, 5, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
        Assert.Equal(12000m, depreciation);
    }
}
