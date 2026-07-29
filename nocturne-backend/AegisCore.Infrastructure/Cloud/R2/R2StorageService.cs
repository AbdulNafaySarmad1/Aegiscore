using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
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

        services.AddSingleton<IR2StorageService, R2StorageService>();
        return services;
    }
}
