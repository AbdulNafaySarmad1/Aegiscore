using AegisCore.Application.ConfigCenter;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Config;
using AegisCore.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class TenantIdentityTests
{
    [Fact]
    public async Task University_Name_Comes_From_The_Wizard_Answer_Until_The_Config_Centre_Overrides_It()
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres: false);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        db.Tenants.Add(new Tenant { Id = tenant, TenantId = tenant, Name = "Fictional National University", ShortName = "FNU", LicenseMode = "demo", IsActive = true });
        await db.SaveChangesAsync();

        // No configuration-centre entry yet: the wizard's answer must win over the placeholder.
        var empty = new Mock<IConfigurationCenterService>();
        empty.Setup(c => c.GetConfigAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new KeyNotFoundException());
        var service = new TenantConfigService(empty.Object, NullLogger<TenantConfigService>.Instance, db);
        Assert.Equal("Fictional National University", service.GetUniversityName());
        Assert.Equal("FNU", service.GetUniversityShortName());

        // A configured value overrides it.
        var configured = new Mock<IConfigurationCenterService>();
        configured.Setup(c => c.GetConfigAsync("identity.university_name", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfigDto { Id = Guid.NewGuid(), Key = "identity.university_name", Value = "Renamed University", Domain = "Identity", CurrentVersion = 1, Status = "Active" });
        configured.Setup(c => c.GetConfigAsync("identity.university_short_name", It.IsAny<CancellationToken>())).ThrowsAsync(new KeyNotFoundException());
        var overridden = new TenantConfigService(configured.Object, NullLogger<TenantConfigService>.Instance, db);
        Assert.Equal("Renamed University", overridden.GetUniversityName());
        Assert.Equal("FNU", overridden.GetUniversityShortName());

        // Anonymous pages have no tenant context; the single active tenant of a per-tenant container still answers.
        await using var anonymous = database.CreateContext(Guid.Empty);
        Assert.Equal("Fictional National University", new TenantConfigService(empty.Object, NullLogger<TenantConfigService>.Instance, anonymous).GetUniversityName());

        // Without a database the placeholder still applies, as before.
        Assert.Equal(DefaultTenantConfigTemplate.DefaultUniversityName, new TenantConfigService(empty.Object, NullLogger<TenantConfigService>.Instance).GetUniversityName());
    }
}
