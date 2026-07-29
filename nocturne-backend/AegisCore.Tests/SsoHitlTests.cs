using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.SsoHitl;

namespace AegisCore.Tests;

public sealed class SsoHitlTests
{
    [Fact]
    public void EmailDomain_Is_Normalized()
    {
        var service = new MultiTenantSsoService(null!);

        Assert.Equal("ssuet.edu.pk", service.GetEmailDomain("Student@SSUET.edu.pk"));
    }

    [Fact]
    public void TemporaryPassword_Is_Complex_And_16_Chars()
    {
        var password = new MultiTenantSsoService(null!).GenerateTemporaryPassword();

        Assert.Equal(16, password.Length);
        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsDigit);
    }

    [Fact]
    public void SsoRedirect_Uses_Microsoft_Tenant()
    {
        var url = new MultiTenantSsoService(null!).BuildRedirectUrl(new UniversitySsoProfile { SsoProvider = "microsoft", SsoTenantId = "tenant", SsoClientId = "client" }, "https://app/callback");

        Assert.Contains("login.microsoftonline.com/tenant", url);
    }

    [Fact]
    public void LowConfidence_Requires_Human_Review()
    {
        Assert.True(new AgenticExamService().RequiresHumanReview(0.74m));
    }

    [Fact]
    public void DestructionManifest_Has_Sha256_Hash()
    {
        var manifest = new AgenticExamService().CreateDestructionManifest(Guid.NewGuid(), 50, Guid.NewGuid());

        Assert.Equal(64, manifest.ManifestHash.Length);
    }
}
