using AegisCore.Application.Foundation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Foundation;

public sealed class LocalDiskStorageProvider(IConfiguration configuration) : IStorageProvider
{
    private readonly string _basePath = Path.GetFullPath(
        configuration["NOCTURNE_STORAGE_LOCAL_PATH"]
        ?? configuration["Storage:LocalPath"]
        ?? Path.Combine(AppContext.BaseDirectory, "storage"));

    public async Task<string> UploadAsync(string path, Stream content, string contentType, CancellationToken ct = default)
    {
        var fullPath = SafePath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var target = File.Create(fullPath);
        await content.CopyToAsync(target, ct);
        return path.Replace('\\', '/');
    }

    public Task<Stream> DownloadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = SafePath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Storage object was not found.", path);
        }

        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = SafePath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
        => Task.FromResult(File.Exists(SafePath(path)));

    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_basePath);
        var root = SafePath(prefix);
        var searchRoot = Directory.Exists(root) ? root : _basePath;

        IReadOnlyList<string> files = Directory
            .EnumerateFiles(searchRoot, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(_basePath, file).Replace('\\', '/'))
            .Where(file => file.StartsWith(prefix.Trim('/'), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult(files);
    }

    private string SafePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Storage path is required.", nameof(path));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_basePath, path.Replace('/', Path.DirectorySeparatorChar)));
        var root = _basePath.EndsWith(Path.DirectorySeparatorChar)
            ? _basePath
            : _basePath + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Storage path escapes the configured storage root.");
        }

        return fullPath;
    }
}

public class FileBackedStorageProvider : IStorageProvider
{
    private readonly LocalDiskStorageProvider _inner;

    public FileBackedStorageProvider(string provider, IConfiguration configuration)
    {
        var root = configuration[$"Storage:{provider}:LocalPath"]
            ?? configuration[$"NOCTURNE_{provider.ToUpperInvariant()}_STORAGE_LOCAL_PATH"]
            ?? Path.Combine(AppContext.BaseDirectory, "storage", provider);
        _inner = new LocalDiskStorageProvider(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["NOCTURNE_STORAGE_LOCAL_PATH"] = root })
            .Build());
    }

    public Task<string> UploadAsync(string path, Stream content, string contentType, CancellationToken ct = default)
        => _inner.UploadAsync(path, content, contentType, ct);

    public Task<Stream> DownloadAsync(string path, CancellationToken ct = default)
        => _inner.DownloadAsync(path, ct);

    public Task DeleteAsync(string path, CancellationToken ct = default)
        => _inner.DeleteAsync(path, ct);

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
        => _inner.ExistsAsync(path, ct);

    public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default)
        => _inner.ListAsync(prefix, ct);
}

public sealed class S3StorageProvider(IConfiguration configuration)
    : FileBackedStorageProvider("s3", configuration);

public sealed class AzureBlobStorageProvider(IConfiguration configuration)
    : FileBackedStorageProvider("azureblob", configuration);

public sealed class SftpStorageProvider(IConfiguration configuration)
    : FileBackedStorageProvider("sftp", configuration);

public static class StorageServiceExtensions
{
    public static IServiceCollection AddStorageProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = (configuration["NOCTURNE_STORAGE_PROVIDER"] ?? configuration["Storage:Provider"] ?? "local").ToLowerInvariant();

        services.AddSingleton<IStorageProvider>(_ => provider switch
        {
            "local" or "disk" or "localdisk" => new LocalDiskStorageProvider(configuration),
            "s3" => new S3StorageProvider(configuration),
            "azure" or "azureblob" => new AzureBlobStorageProvider(configuration),
            "sftp" => new SftpStorageProvider(configuration),
            _ => throw new InvalidOperationException($"Unknown storage provider '{provider}'.")
        });

        return services;
    }
}
