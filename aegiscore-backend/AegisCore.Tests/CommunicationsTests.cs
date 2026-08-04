using AegisCore.Infrastructure.Platform;

namespace AegisCore.Tests;

public sealed class CommunicationsTests
{
    [Fact]
    public void Template_Renderer_Replaces_Double_Brace_Variables()
    {
        var rendered = NotificationPlatformService.ApplyVariables(
            "Fee {{amount}} is due on {{due_date}}.",
            new Dictionary<string, string> { ["amount"] = "25000", ["due_date"] = "2026-08-15" });

        Assert.Equal("Fee 25000 is due on 2026-08-15.", rendered);
    }
}
