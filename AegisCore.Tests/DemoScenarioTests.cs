using AegisCore.Infrastructure.Deployment.Seeders;

namespace AegisCore.Tests;

public sealed class DemoScenarioTests
{
    [Fact]
    public void DefaultScenario_IsValidatedAndUsesSemanticReferences()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../demo-scenarios"));
        var scenario = DemoScenario.Load(root, "pakistan-bscs-v1");
        Assert.Equal("Asia/Karachi", scenario.Institution.Timezone);
        Assert.Equal("PKR", scenario.Institution.Currency);
        var cohort = scenario.BuildCohort();
        Assert.Equal(50, cohort.Count);
        Assert.Equal("Aisha Khan", $"{cohort[0].FirstName} {cohort[0].LastName}");
        Assert.Equal(50, cohort.Select(member => member.Email).Distinct().Count());
        Assert.Equal(130, scenario.Courses.Sum(course => course.Credits));
        Assert.Equal(scenario.Fee.Total, scenario.Fee.Components!.Sum(component => component.Amount));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("nested/scenario")]
    public void ScenarioName_RejectsPathTraversal(string name)
    {
        Assert.Throws<InvalidOperationException>(() => DemoScenario.Load(Path.GetTempPath(), name));
    }
}
