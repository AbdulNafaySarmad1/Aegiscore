namespace AegisCore.Tests;

public sealed class SetupControllerRouteTests
{
    [Fact]
    public void SetupController_Exposes_Wizard_Test_Routes_And_Does_Not_Skip_Connection_Tests()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../AegisCore.API/Controllers/SetupController.cs"));

        Assert.Contains("[HttpPost(\"step3/test-postgres\")]", source);
        Assert.Contains("[HttpPost(\"step4/test-redis\")]", source);
        Assert.Contains("[HttpPost(\"step7/test-smtp\")]", source);
        Assert.DoesNotContain("connection test skipped", source);
    }
}
