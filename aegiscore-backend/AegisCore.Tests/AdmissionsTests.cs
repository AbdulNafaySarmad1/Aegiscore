using AegisCore.Infrastructure.Admissions;

namespace AegisCore.Tests;

public sealed class AdmissionsTests
{
    [Fact]
    public void IbccEquivalence_Converts_And_Clamps()
    {
        var service = new IBCCEquivalenceService();

        Assert.Equal(81m, service.ConvertOLevelToMatric(90m));
        Assert.Equal(85m, service.ConvertALevelToFsc(100m));
        Assert.Equal(100m, service.ConvertOLevelToMatric(200m));
    }
}
