using AegisCore.Application.Marketplace;

namespace AegisCore.Tests;

public sealed class MarketplaceRevenueTests
{
    [Theory]
    [InlineData(100, 30, 70)]
    [InlineData(99.99, 30.00, 69.99)]
    public void RevenueSplit_IsThirtySeventy(decimal gross, decimal aegiscore, decimal developer)
    {
        var split = MarketplaceRevenueCalculator.Split(gross);

        Assert.Equal(aegiscore, split.AegisCoreAmount);
        Assert.Equal(developer, split.DeveloperAmount);
    }
}
