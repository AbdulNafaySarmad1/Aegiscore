using AegisCore.Infrastructure.Installation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace AegisCore.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class InstallationStateCollection
{
    public const string Name = "Installation state";
}

[Collection(InstallationStateCollection.Name)]
public sealed class InstallationStateServiceTests
{
    private const string MarkerEnvironmentVariable = "AEGISCORE_INSTALLATION_COMPLETED";

    [Fact]
    public async Task MarkerWrittenByWizardSurvivesProcessRestart()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"aegiscore-install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(contentRoot);
        var previousMarker = Environment.GetEnvironmentVariable(MarkerEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(MarkerEnvironmentVariable, null);
            var configuration = new ConfigurationBuilder().Build();
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(contentRoot);
            using var services = new ServiceCollection().BuildServiceProvider();

            var installer = new InstallationStateService(configuration, environment.Object, services);
            await installer.MarkAsInstalledAsync();

            Environment.SetEnvironmentVariable(MarkerEnvironmentVariable, null);
            var restartedService = new InstallationStateService(configuration, environment.Object, services);

            Assert.True(restartedService.IsInstalled());
            Assert.True(InstallationStateService.HasPersistentMarker(configuration, contentRoot));
        }
        finally
        {
            Environment.SetEnvironmentVariable(MarkerEnvironmentVariable, previousMarker);
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public void CorruptMarkerDoesNotEnableProductionMode()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), $"aegiscore-install-{Guid.NewGuid():N}");
        var configDirectory = Path.Combine(contentRoot, "config");
        Directory.CreateDirectory(configDirectory);
        File.WriteAllText(Path.Combine(configDirectory, "installation.json"), "{not-json");
        var previousMarker = Environment.GetEnvironmentVariable(MarkerEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(MarkerEnvironmentVariable, null);
            var configuration = new ConfigurationBuilder().Build();

            Assert.False(InstallationStateService.HasPersistentMarker(configuration, contentRoot));
        }
        finally
        {
            Environment.SetEnvironmentVariable(MarkerEnvironmentVariable, previousMarker);
            Directory.Delete(contentRoot, recursive: true);
        }
    }
}
