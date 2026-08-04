using AegisCore.Application.MicrosoftEcosystem;

namespace AegisCore.Tests;

public sealed class MicrosoftEcosystemTests
{
    [Theory]
    [InlineData("vc")]
    [InlineData("hod")]
    [InlineData("faculty")]
    [InlineData("finance")]
    public void PowerBi_Roles_Include_Required_Dashboards(string role)
    {
        Assert.Contains(role, PowerBiRoles.Allowed);
    }
}
