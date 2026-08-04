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

    [Fact]
    public void NextPortal_Owns_Setup_Page_While_Api_Owns_Only_Setup_Endpoints()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "../../../..");
        var nginx = File.ReadAllText(Path.Combine(root, "AegisCore.Portal/nginx.conf"));
        var compose = File.ReadAllText(Path.Combine(root, "docker-compose.yml"));
        var workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/deploy.yml"));

        Assert.Contains("src/app/setup/page.tsx", Directory.GetFiles(
            Path.Combine(root, "AegisCore.Portal/src/app/setup"),
            "page.tsx",
            SearchOption.TopDirectoryOnly).Single().Replace('\\', '/'));
        Assert.Contains("^/(api|hubs|swagger|health)", nginx);
        Assert.DoesNotContain("api|hubs|setup|swagger", nginx);
        Assert.Contains("AEGISCORE_INSTALLATION_COMPLETED: ${AEGISCORE_INSTALLATION_COMPLETED:-false}", compose);
        Assert.Contains("AEGISCORE_INSTALLATION_COMPLETED=true", workflow);
    }
}
