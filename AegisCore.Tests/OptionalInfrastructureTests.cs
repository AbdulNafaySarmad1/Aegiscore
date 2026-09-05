using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class OptionalInfrastructureTests
{
    [Fact]
    public void DomainProvisioning_DoesNotRequireCloudflareTunnel()
    {
        var service = new OnboardingService(
            new ConfigurationBuilder().Build(),
            NullLogger<OnboardingService>.Instance);
        var state = new WizardStateDto
        {
            TenantId = Guid.NewGuid(),
            UniversityShortName = "ACU"
        };

        var direct = service.ProvisionDomain(state, "example.edu");

        Assert.Equal("https://erp.example.edu", direct.ErpUrl);
        Assert.Null(direct.CnameRecord);
        Assert.False(state.UseCloudflareTunnel);
        Assert.Null(state.CloudflareTunnelId);
        Assert.Contains("university-managed ingress", service.GenerateDnsInstructions(state), StringComparison.Ordinal);

        var tunnel = service.ProvisionDomain(state, "example.edu", useCloudflareTunnel: true);

        Assert.True(state.UseCloudflareTunnel);
        Assert.NotNull(state.CloudflareTunnelId);
        Assert.Equal("CNAME", tunnel.CnameRecord?.Type);
        Assert.EndsWith(".cfargotunnel.com", tunnel.CnameRecord?.Value, StringComparison.Ordinal);
    }
}
