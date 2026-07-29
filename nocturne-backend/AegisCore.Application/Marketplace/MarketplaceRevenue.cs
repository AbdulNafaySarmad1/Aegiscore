namespace AegisCore.Application.Marketplace;

public static class MarketplaceRevenueCalculator
{
    public static RevenueSplit Split(decimal grossAmount)
    {
        if (grossAmount < 0) throw new ArgumentOutOfRangeException(nameof(grossAmount), "Gross amount cannot be negative.");
        var nocturne = decimal.Round(grossAmount * 0.30m, 2, MidpointRounding.AwayFromZero);
        return new RevenueSplit(grossAmount, nocturne, grossAmount - nocturne);
    }
}

public sealed record RevenueSplit(decimal GrossAmount, decimal NocturneAmount, decimal DeveloperAmount);

public sealed record RecordPluginRevenueRequest(Guid ExtensionId, Guid TenantId, decimal GrossAmount);
