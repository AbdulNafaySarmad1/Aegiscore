namespace AegisCore.Tests;

public sealed class IdentityPatchTests
{
    [Fact]
    public void CreateRole_Does_Not_Call_PermissionRepository_With_Null()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../AegisCore.API/Controllers/IdentityController.cs"));

        Assert.DoesNotContain("AddAsync(null!", source);
        Assert.Contains("RolePermissions.Add", source);
    }
}
