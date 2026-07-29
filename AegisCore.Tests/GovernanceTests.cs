using AegisCore.Infrastructure.Governance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class GovernanceTests
{
    [Theory]
    [InlineData(null, 12)]
    [InlineData(0, 1)]
    [InlineData(73, 72)]
    public void ReplicationLag_Is_Clamped(int? requested, int expected)
    {
        var service = NewService();

        Assert.Equal(expected, service.NormalizeReplicationLag(requested));
    }

    [Theory]
    [InlineData(100, 105, "hod")]
    [InlineData(100, 115, "vc")]
    [InlineData(100, 116, "chancellor")]
    public void GradeOverrideThreshold_Uses_Percent_Delta(int original, int next, string expected)
    {
        var service = NewService();

        Assert.Equal(expected, service.DetermineGradeApprovalThreshold(original, next));
    }

    [Fact]
    public void DynamicFee_Never_Goes_Negative()
    {
        var service = NewService();

        Assert.Equal(0m, service.CalculateDynamicFee(3, 1000m, 5000m, 0m));
    }

    [Fact]
    public void SectionCapacity_Checks_Regular_And_Exam_Limits()
    {
        var service = NewService();

        Assert.False(service.IsSectionCapacityAllowed(59, 80, 60, 80));
    }

    private static GovernanceService NewService()
        => new(null!, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Governance:FeedbackEncryptionKeyBase64"] = Convert.ToBase64String(new byte[32])
        }).Build(), NullLogger<GovernanceService>.Instance);
}
