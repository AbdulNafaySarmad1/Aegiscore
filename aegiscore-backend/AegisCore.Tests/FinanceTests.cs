using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;

namespace AegisCore.Tests;

public sealed class FinanceTests
{
    [Fact]
    public void PaymentRequest_Preserves_Amount_And_Method()
    {
        var request = new PaymentRequest("jazzcash", 12500m, "TXN-1");

        Assert.Equal("jazzcash", request.Method);
        Assert.Equal(12500m, request.Amount);
    }

    [Fact]
    public void FeeStructure_Total_Can_Be_Component_Driven()
    {
        var structure = new FeeStructure
        {
            Components =
            [
                new FeeComponent { Amount = 1000m, IsMandatory = true },
                new FeeComponent { Amount = 500m, IsMandatory = false }
            ]
        };

        Assert.Equal(1000m, structure.Components.Where(c => c.IsMandatory).Sum(c => c.Amount));
    }
}
