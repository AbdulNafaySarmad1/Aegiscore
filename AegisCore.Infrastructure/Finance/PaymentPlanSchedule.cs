using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.Finance;

public static class FinancePaymentPlanSchedule
{
    public static List<FeeInstallment> Build(FinancePaymentPlan plan, int count)
    {
        var amount = Math.Round(plan.TotalAmount / count, 4, MidpointRounding.ToEven);
        var installments = new List<FeeInstallment>(count);
        for (var index = 0; index < count; index++)
        {
            var dueDate = plan.PlanType switch
            {
                "weekly" => plan.StartDate.AddDays(index * 7),
                "monthly" => plan.StartDate.AddMonths(index),
                "semester-split" => plan.StartDate.AddMonths(index * 3),
                _ => plan.StartDate
            };
            var installmentAmount = index == count - 1 ? plan.TotalAmount - amount * (count - 1) : amount;
            installments.Add(new FeeInstallment
            {
                Id = Guid.NewGuid(), TenantId = plan.TenantId, FeeDemandId = plan.FeeDemandId,
                Sequence = index + 1, Amount = installmentAmount, DueDate = dueDate
            });
        }
        return installments;
    }
}
