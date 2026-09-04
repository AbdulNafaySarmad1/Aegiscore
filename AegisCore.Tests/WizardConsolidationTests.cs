namespace AegisCore.Tests;

public sealed class WizardConsolidationTests
{
    [Fact]
    public void WizardController_IsTheOnlySevenStepSetupBackend()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "../../../..");
        var controllerDirectory = Path.Combine(root, "AegisCore.API/Controllers");
        var source = File.ReadAllText(Path.Combine(controllerDirectory, "WizardController.cs"));

        Assert.Single(Directory.GetFiles(controllerDirectory, "*WizardController.cs", SearchOption.TopDirectoryOnly));
        Assert.Contains("[HttpPost(\"step2/test\")]", source);
        Assert.Contains("[HttpPost(\"step4/permissions\")]", source);
        Assert.Contains("[HttpPost(\"step6/import/{fileType}/validate\")]", source);
        Assert.Contains("if (step is < 1 or > 7)", source);
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
