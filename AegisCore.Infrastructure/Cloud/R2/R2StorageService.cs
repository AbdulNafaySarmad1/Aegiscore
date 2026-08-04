using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using AegisCore.Application.Foundation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AegisCore.Infrastructure.Cloud.R2;

public interface IR2StorageService
{
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

public class R2StorageOptions
{
    public string AccountId { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BucketName { get; set; } = "aegiscore";
    public string Endpoint { get; set; } = "";
}

public class R2StorageService : IR2StorageService, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly R2StorageOptions _options;
    private readonly ILogger<R2StorageService> _logger;

    public R2StorageService(IOptions<R2StorageOptions> options, ILogger<R2StorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(_options.AccessKey) || string.IsNullOrWhiteSpace(_options.SecretKey)
            || string.IsNullOrWhiteSpace(_options.BucketName))
            throw new InvalidOperationException("Cloudflare R2 endpoint, bucket, and credentials must be configured when R2 storage is selected.");

        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.USEast1,
            ServiceURL = _options.Endpoint,
            ForcePathStyle = true
        };

        _client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        await _client.PutObjectAsync(request, cancellationToken);
        return $"{_options.Endpoint.TrimEnd('/')}/{_options.BucketName}/{key}";
    }

    public async Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_options.BucketName, key, cancellationToken);
            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            return ms;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(_options.BucketName, key, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public void Dispose() => _client?.Dispose();
}

public sealed class StorageProviderR2Adapter(IStorageProvider storage) : IR2StorageService
{
    public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
        => storage.UploadAsync(key, content, contentType, cancellationToken);

    public async Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        try { return await storage.DownloadAsync(key, cancellationToken); }
        catch (FileNotFoundException) { return null; }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => storage.DeleteAsync(key, cancellationToken);

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => storage.ExistsAsync(key, cancellationToken);
}

public static class R2StorageServiceExtensions
{
    public static IServiceCollection AddR2Storage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<R2StorageOptions>(options =>
        {
            options.AccountId = configuration["R2:AccountId"] ?? "";
            options.AccessKey = configuration["R2:AccessKey"] ?? "";
            options.SecretKey = configuration["R2:SecretKey"] ?? "";
            options.BucketName = configuration["R2:BucketName"] ?? "aegiscore";
            options.Endpoint = configuration["R2:Endpoint"]
                ?? $"https://{configuration["R2:AccountId"]}.r2.cloudflarestorage.com";
        });

        var provider = (configuration["AEGISCORE_STORAGE_PROVIDER"] ?? configuration["Storage:Provider"] ?? "local").Trim().ToLowerInvariant();
        services.AddSingleton<IR2StorageService>(serviceProvider => provider is "r2" or "cloudflare-r2"
            ? ActivatorUtilities.CreateInstance<R2StorageService>(serviceProvider)
            : new StorageProviderR2Adapter(serviceProvider.GetRequiredService<IStorageProvider>()));
        return services;
    }
}
