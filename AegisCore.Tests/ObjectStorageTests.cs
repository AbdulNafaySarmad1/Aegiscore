using System.Security.Cryptography;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure.Cloud.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// Storage used to be four implementations that returned a plausible URL and threw the bytes away,
/// which meant a vaulted degree certificate, a subject-access export and an uploaded logo were all
/// lost while every screen and health check reported success. These tests exist so that cannot
/// return: a provider must hand back exactly what it was given, and a provider this build cannot
/// talk to must refuse rather than accept and discard.
/// </summary>
public class ObjectStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "aegis-storage-test-" + Guid.NewGuid().ToString("N")[..8]);

    private LocalFileStorageService Local() => new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AEGISCORE_STORAGE_LOCAL_PATH"] = _root }).Build(),
        NullLogger<LocalFileStorageService>.Instance);

    [Fact]
    public async Task Stored_Bytes_Come_Back_Byte_For_Byte()
    {
        var storage = Local();
        var content = RandomNumberGenerator.GetBytes(64 * 1024);

        await storage.CreateBucketIfNotExistsAsync("vault");
        await storage.UploadAsync("vault", "tier1/2026/09/degree.pdf", new MemoryStream(content), "application/pdf");

        await using var read = await storage.DownloadAsync("vault", "tier1/2026/09/degree.pdf");
        using var buffer = new MemoryStream();
        await read.CopyToAsync(buffer);

        Assert.Equal(content, buffer.ToArray());
        Assert.True(await storage.ExistsAsync("vault", "tier1/2026/09/degree.pdf"));
    }

    [Fact]
    public async Task An_Object_That_Was_Never_Stored_Is_Not_Quietly_Empty()
    {
        var storage = Local();
        // The replaced providers answered this with a zero-byte stream, which a caller could not
        // tell apart from an empty file.
        await Assert.ThrowsAsync<FileNotFoundException>(() => storage.DownloadAsync("vault", "never/written.pdf"));
    }

    [Fact]
    public async Task A_Key_Cannot_Walk_Out_Of_The_Storage_Root()
    {
        var storage = Local();
        await storage.CreateBucketIfNotExistsAsync("vault");
        await storage.UploadAsync("vault", "../../escaped.txt", new MemoryStream([1, 2, 3]), "text/plain");

        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(_root)!, "escaped.txt"));
        Assert.Single(Directory.GetFiles(_root, "escaped.txt", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Deleting_Removes_The_Object()
    {
        var storage = Local();
        await storage.CreateBucketIfNotExistsAsync("vault");
        await storage.UploadAsync("vault", "temp.txt", new MemoryStream([7]), "text/plain");
        await storage.DeleteAsync("vault", "temp.txt");
        Assert.False(await storage.ExistsAsync("vault", "temp.txt"));
    }

    [Theory]
    [InlineData("local")]
    [InlineData("ONPREMISE")]
    [InlineData("disk")]
    public void Local_Spellings_All_Select_The_Filesystem_Provider(string provider)
        => Assert.IsType<LocalFileStorageService>(ObjectStorageFactory.Create(Config(provider), NullLoggerFactory.Instance));

    [Theory]
    [InlineData("AZURE")]
    [InlineData("GCP")]
    [InlineData("dropbox")]
    public void A_Provider_This_Build_Cannot_Store_With_Refuses_At_Startup(string provider)
    {
        // Silently accepting and discarding was the old behaviour; the refusal has to name the way out.
        var error = Assert.Throws<InvalidOperationException>(() => ObjectStorageFactory.Validate(Config(provider)));
        Assert.Contains("local", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("s3", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_Cloud_Provider_Decides_When_Storage_Is_Not_Set_Explicitly()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AEGISCORE_CLOUD_PROVIDER"] = "ONPREMISE",
            ["AEGISCORE_STORAGE_LOCAL_PATH"] = _root
        }).Build();
        Assert.IsType<LocalFileStorageService>(ObjectStorageFactory.Create(configuration, NullLoggerFactory.Instance));
    }

    private IConfiguration Config(string provider) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["AEGISCORE_STORAGE_PROVIDER"] = provider,
        ["AEGISCORE_STORAGE_LOCAL_PATH"] = _root
    }).Build();

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}
