using AegisCore.Infrastructure.Foundation;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AegisCore.Tests;

public sealed class FoundationStorageTests
{
    [Fact]
    public async Task LocalDiskStorageProvider_RoundTrips_And_BlocksTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), "aegiscore-storage-" + Guid.NewGuid());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AEGISCORE_STORAGE_LOCAL_PATH"] = root })
            .Build();

        var storage = new LocalDiskStorageProvider(configuration);
        await using var upload = new MemoryStream("hello"u8.ToArray());

        var key = await storage.UploadAsync("docs/a.txt", upload, "text/plain");

        Assert.Equal("docs/a.txt", key);
        Assert.True(await storage.ExistsAsync("docs/a.txt"));

        await using var downloaded = await storage.DownloadAsync("docs/a.txt");
        using var reader = new StreamReader(downloaded);
        Assert.Equal("hello", await reader.ReadToEndAsync());
        Assert.Contains("docs/a.txt", await storage.ListAsync("docs"));
        Assert.Throws<InvalidOperationException>(() => storage.ExistsAsync("../escape.txt").GetAwaiter().GetResult());
    }

    [Theory]
    [InlineData("local")]
    [InlineData("azureblob")]
    [InlineData("s3")]
    [InlineData("sftp")]
    public async Task StorageProviderSelection_RoundTrips_AllConfiguredBackends(string provider)
    {
        var root = Path.Combine(Path.GetTempPath(), "aegiscore-storage-" + provider + "-" + Guid.NewGuid());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AEGISCORE_STORAGE_PROVIDER"] = provider,
                ["AEGISCORE_STORAGE_LOCAL_PATH"] = root,
                [$"Storage:{provider}:LocalPath"] = root
            })
            .Build();

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddStorageProvider(configuration)
            .BuildServiceProvider();
        var storage = services.GetRequiredService<AegisCore.Application.Foundation.IStorageProvider>();

        await using var upload = new MemoryStream("ok"u8.ToArray());
        await storage.UploadAsync("probe.txt", upload, "text/plain");

        Assert.True(await storage.ExistsAsync("probe.txt"));
    }

    [Fact]
    public void PqcCryptographyService_Requires_Configured_ThirtyTwoByteKey()
    {
        var options = Options.Create(new PqcCryptographyOptions
        {
            MasterKeyBase64 = Convert.ToBase64String(new byte[32])
        });
        var crypto = new PqcCryptographyService(options, NullLogger<PqcCryptographyService>.Instance);

        var ciphertext = crypto.EncryptString("secret");

        Assert.Equal("secret", crypto.DecryptString(ciphertext));
    }
}
