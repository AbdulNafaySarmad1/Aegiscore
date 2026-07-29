using AegisCore.Infrastructure.StudentLifecycle;

namespace AegisCore.Tests;

public sealed class StudentLifecycleTests
{
    [Fact]
    public void Placement_Eligibility_Requires_Cgpa_Completion_And_No_Failed_Courses()
    {
        var service = new StudentLifecycleService();

        Assert.True(service.IsPlacementEligible(3.1m, 104, 130, 0, 2.5m));
        Assert.False(service.IsPlacementEligible(3.1m, 90, 130, 0, 2.5m));
        Assert.False(service.IsPlacementEligible(3.1m, 104, 130, 1, 2.5m));
    }

    [Fact]
    public void Activity_Points_Reward_Completed_Organizer_Work()
    {
        var service = new StudentLifecycleService();
        Assert.Equal(10, service.CalculateActivityPoints("organizer", true));
        Assert.Equal(0, service.CalculateActivityPoints("organizer", false));
    }
}
