using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.Finance;

public static class AssetDepreciationCalculator
{
    public static decimal MonthlyCharge(Asset asset, DateTime periodEnd)
    {
        var purchaseDate = (asset.PurchaseDate ?? asset.CreatedAt).Date;
        var end = periodEnd.Date;
        if (asset.PurchaseCost <= 0m || asset.UsefulLifeYears <= 0 || end < purchaseDate) return 0m;
        var monthStart = new DateTime(end.Year, end.Month, 1);
        var current = TotalDepreciation(asset, purchaseDate, end);
        var prior = TotalDepreciation(asset, purchaseDate, monthStart.AddDays(-1));
        return Math.Round(Math.Max(0m, current - prior), 4, MidpointRounding.ToEven);
    }

    public static decimal TotalDepreciation(Asset asset, DateTime purchaseDate, DateTime asOf)
    {
        if (asOf.Date < purchaseDate.Date || asset.PurchaseCost <= 0m || asset.UsefulLifeYears <= 0) return 0m;
        return asset.DepreciationMethod == DepreciationMethod.ReducingBalance
            ? ReducingBalance(asset.PurchaseCost, purchaseDate.Date, asOf.Date)
            : StraightLine(asset.PurchaseCost, purchaseDate.Date, asOf.Date, asset.UsefulLifeYears);
    }

    private static decimal StraightLine(decimal cost, DateTime purchaseDate, DateTime asOf, int years)
    {
        var elapsedDays = (asOf - purchaseDate).Days + 1m;
        return Math.Min(cost, Math.Round(cost * elapsedDays / (years * 365m), 4, MidpointRounding.ToEven));
    }

    private static decimal ReducingBalance(decimal cost, DateTime purchaseDate, DateTime asOf)
    {
        var book = cost;
        var month = new DateTime(purchaseDate.Year, purchaseDate.Month, 1);
        var lastMonth = new DateTime(asOf.Year, asOf.Month, 1);
        while (month <= lastMonth && book > 0m)
        {
            var monthEnd = month.AddMonths(1).AddDays(-1);
            var firstDay = purchaseDate > month ? purchaseDate : month;
            var lastDay = asOf < monthEnd ? asOf : monthEnd;
            if (lastDay >= firstDay)
            {
                var fraction = (lastDay - firstDay).Days + 1m;
                var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
                var charge = Math.Min(book, Math.Round(book * 0.20m / 12m * fraction / daysInMonth, 4, MidpointRounding.ToEven));
                book -= charge;
            }
            month = month.AddMonths(1);
        }
        return cost - book;
    }
}
