namespace AegisCore.Infrastructure.Finance;

public static class PaymentPlanPenaltyCalculator
{
    public static decimal Calculate(decimal outstanding, decimal flatAmount, decimal percentage)
    {
        if (outstanding <= 0m || flatAmount < 0m || percentage < 0m) return 0m;
        return Math.Round(flatAmount + outstanding * percentage / 100m, 4, MidpointRounding.ToEven);
    }
}
