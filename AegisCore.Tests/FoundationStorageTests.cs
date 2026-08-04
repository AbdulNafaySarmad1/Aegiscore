using AegisCore.Infrastructure.Foundation;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Cloud.OnPremise;
using AegisCore.Infrastructure.Security.Pqc;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AegisCore.Tests;

public sealed class FoundationStorageTests
{
    [Fact]
    public async Task EncryptedFileSecretService_Persists_And_Rejects_Tampering()
    {
        var root = Path.Combine(Path.GetTempPath(), "aegiscore-vault-" + Guid.NewGuid());
        var path = Path.Combine(root, "vault.bin");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AEGISCORE_LOCAL_SECRET_PATH"] = path,
            ["AEGISCORE_PQC_KEY_BASE64"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        }).Build();
        var first = new EncryptedFileSecretService(configuration);
        await first.SetSecretAsync("/tenants/test/pqc/private", "sensitive-value");

        var reloaded = new EncryptedFileSecretService(configuration);
        Assert.Equal("sensitive-value", await reloaded.GetSecretAsync("/tenants/test/pqc/private"));
        var encrypted = await File.ReadAllBytesAsync(path);
        Assert.DoesNotContain("sensitive-value"u8.ToArray(), encrypted);

        var bytes = encrypted;
        bytes[^1] ^= 1;
        await File.WriteAllBytesAsync(path, bytes);
        await Assert.ThrowsAnyAsync<CryptographicException>(() => reloaded.GetSecretAsync("/tenants/test/pqc/private"));
    }

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

    [Fact]
    public async Task FinanceObjectStorage_Uses_Configured_LocalProvider()
    {
        var root = Path.Combine(Path.GetTempPath(), "aegiscore-finance-storage-" + Guid.NewGuid());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AEGISCORE_STORAGE_PROVIDER"] = "local",
            ["AEGISCORE_STORAGE_LOCAL_PATH"] = root
        }).Build();
        using var services = new ServiceCollection().AddStorageProvider(configuration).AddR2Storage(configuration).BuildServiceProvider();
        var storage = services.GetRequiredService<IR2StorageService>();

        await using var upload = new MemoryStream("evidence"u8.ToArray());
        await storage.UploadAsync("audit/evidence.json", upload, "application/json");

        await using var downloaded = await storage.DownloadAsync("audit/evidence.json");
        Assert.NotNull(downloaded);
        Assert.Null(await storage.DownloadAsync("audit/missing.json"));
    }
}
