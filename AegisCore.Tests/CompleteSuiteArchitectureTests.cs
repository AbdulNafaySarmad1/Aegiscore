using AegisCore.Application.Licensing;

namespace AegisCore.Tests;

public sealed class CompleteSuiteArchitectureTests
{
    [Fact]
    public void License_contract_does_not_expose_module_entitlements()
    {
        Assert.DoesNotContain(
            typeof(ILicenseService).GetMethods(),
            method => method.Name.Contains("Module", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            typeof(LicenseInfo).GetProperties(),
            property => property.Name.Contains("Module", StringComparison.OrdinalIgnoreCase));
    }
}
