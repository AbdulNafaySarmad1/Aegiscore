namespace AegisCore.Tests;

public sealed class LoginAuditControllerTests
{
    [Fact]
    public void LoginStats_Uses_Audit_Repository_Data()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../AegisCore.API/Controllers/LoginAuditController.cs"));

        Assert.DoesNotContain("totalLogins = 0", source);
        Assert.DoesNotContain("failedAttempts = 0", source);
        Assert.Contains("await _repo.GetAllAsync", source);
        Assert.Contains("GroupBy(r => r.UserRole)", source);
        Assert.Contains("GroupBy(r => r.Provider.ToString())", source);
    }
}
