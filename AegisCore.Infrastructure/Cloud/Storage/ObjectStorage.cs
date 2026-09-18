using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AegisCore.Contracts.Abstractions.Cloud;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Cloud.Storage;

/// <summary>
/// Object storage that actually stores the object.
///
/// Every earlier implementation of <see cref="IStorageService"/> in this solution returned a
/// plausible URL and discarded the bytes, so a degree certificate uploaded to the document vault,
/// a subject-access export, a profile picture and an institution's logo were all accepted, recorded
/// in the database, and then gone. These two providers are the real thing: the local one writes to a
/// mounted volume, and the S3 one talks to S3, Cloudflare R2, MinIO or any S3-compatible endpoint.
/// A provider that is not implemented refuses at startup rather than silently swallowing files.
/// </summary>
public sealed class LocalFileStorageService : IStorageService
{
    private readonly string _root;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _root = Path.GetFullPath(
            configuration["AEGISCORE_STORAGE_LOCAL_PATH"]
            ?? configuration["Storage:LocalPath"]
            ?? (OperatingSystem.IsWindows()
                ? Path.Combine(AppContext.BaseDirectory, "storage")
                : "/app/storage"));
        Directory.CreateDirectory(_root);
        _logger.LogInformation("Object storage: local filesystem at {Root}", _root);
    }

    /// <summary>The directory files land in. Shown by the health check so an operator can find them.</summary>
    public string Root => _root;

    public async Task<string> UploadAsync(string bucket, string key, Stream content, string contentType, StorageVisibility visibility = StorageVisibility.Private, CancellationToken cancellationToken = default)
    {
        var path = Resolve(bucket, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Write beside the target and move into place, so a crash mid-write cannot leave a
        // half-written file that later verifies as tampered.
        var staging = path + ".part-" + Guid.NewGuid().ToString("N")[..8];
        try
        {
            await using (var file = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                if (content.CanSeek) content.Position = 0;
                await content.CopyToAsync(file, cancellationToken);
                await file.FlushAsync(cancellationToken);
            }
            File.Move(staging, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(staging)) File.Delete(staging);
            throw;
        }

        await File.WriteAllTextAsync(path + ".type", contentType ?? "application/octet-stream", cancellationToken);
        _logger.LogInformation("Stored {Bytes} bytes at {Bucket}/{Key}", new FileInfo(path).Length, bucket, key);
        return $"file://{path.Replace('\\', '/')}";
    }

    public Task<Stream> DownloadAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(bucket, key);
        if (!File.Exists(path))
            throw new FileNotFoundException($"No stored object at {bucket}/{key}.", path);
        return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public Task DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(bucket, key);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".type")) File.Delete(path + ".type");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Local storage has no signed-URL scheme: a caller that needs the bytes downloads them
    /// through the API, which is where the access check and the audit entry live.
    /// </summary>
    public Task<string> GetPresignedUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Local file storage issues no pre-signed URLs. Download the object through the API, which checks access and records who read it, or configure an S3-compatible provider.");

    public Task<bool> ExistsAsync(string bucket, string key, CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(Resolve(bucket, key)));

    public Task CreateBucketIfNotExistsAsync(string bucket, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.Combine(_root, Sanitize(bucket)));
        return Task.CompletedTask;
    }

    private string Resolve(string bucket, string key)
    {
        var segments = key.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Sanitize);
        var path = Path.GetFullPath(Path.Combine(new[] { _root, Sanitize(bucket) }.Concat(segments).ToArray()));
        // A key is caller-supplied; it must not be able to walk out of the storage root.
        if (!path.StartsWith(_root, StringComparison.Ordinal))
            throw new ArgumentException("Storage key resolves outside the storage root.", nameof(key));
        return path;
    }

    private static string Sanitize(string segment)
    {
        if (segment is "." or "..") return "_";
        var sb = new StringBuilder(segment.Length);
        foreach (var c in segment)
            sb.Append(char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '+' or '=' or '@' or ' ' ? c : '_');
        var cleaned = sb.ToString().Trim();
        return cleaned.Length == 0 ? "_" : cleaned;
    }
}

/// <summary>
/// S3 and anything that speaks S3: AWS, Cloudflare R2, MinIO, Wasabi, Ceph. One provider covers
/// them all because they differ only in endpoint and credentials.
/// </summary>
public sealed class S3CompatibleStorageService : IStorageService, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly ILogger _logger;
    private readonly string _describe;

    public S3CompatibleStorageService(IConfiguration configuration, ILogger logger)
    {
        _logger = logger;
        var accessKey = configuration["AEGISCORE_STORAGE_ACCESS_KEY"] ?? configuration["AWS_ACCESS_KEY_ID"] ?? configuration["R2_ACCESS_KEY_ID"];
        var secretKey = configuration["AEGISCORE_STORAGE_SECRET_KEY"] ?? configuration["AWS_SECRET_ACCESS_KEY"] ?? configuration["R2_SECRET_ACCESS_KEY"];
        var serviceUrl = configuration["AEGISCORE_STORAGE_ENDPOINT"] ?? configuration["R2_ENDPOINT"] ?? configuration["S3_ENDPOINT"];
        var region = configuration["AEGISCORE_STORAGE_REGION"] ?? configuration["AWS_REGION"] ?? "us-east-1";

        var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(region) };
        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            // R2 and MinIO need path-style addressing and an explicit endpoint.
            config.ServiceURL = serviceUrl;
            config.ForcePathStyle = true;
            config.RegionEndpoint = null;
            config.AuthenticationRegion = region;
        }

        _client = string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey)
            ? new AmazonS3Client(config)                                               // instance profile / ambient credentials
            : new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config);
        _describe = serviceUrl ?? $"s3 ({region})";
        _logger.LogInformation("Object storage: S3-compatible endpoint {Endpoint}", _describe);
    }

    public async Task<string> UploadAsync(string bucket, string key, Stream content, string contentType, StorageVisibility visibility = StorageVisibility.Private, CancellationToken cancellationToken = default)
    {
        if (content.CanSeek) content.Position = 0;
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true,
            CannedACL = visibility == StorageVisibility.Public ? S3CannedACL.PublicRead : S3CannedACL.Private
        }, cancellationToken);
        return $"{_describe.TrimEnd('/')}/{bucket}/{key}";
    }

    public async Task<Stream> DownloadAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetObjectAsync(bucket, key, cancellationToken);
        var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    public Task DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default)
        => _client.DeleteObjectAsync(bucket, key, cancellationToken);

    public Task<string> GetPresignedUrlAsync(string bucket, string key, TimeSpan expiry, CancellationToken cancellationToken = default)
        => _client.GetPreSignedURLAsync(new GetPreSignedUrlRequest { BucketName = bucket, Key = key, Expires = DateTime.UtcNow.Add(expiry) });

    public async Task<bool> ExistsAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        try { await _client.GetObjectMetadataAsync(bucket, key, cancellationToken); return true; }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return false; }
    }

    public async Task CreateBucketIfNotExistsAsync(string bucket, CancellationToken cancellationToken = default)
    {
        try { await _client.GetBucketLocationAsync(bucket, cancellationToken); }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await _client.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, cancellationToken);
            _logger.LogInformation("Created storage bucket {Bucket}", bucket);
        }
    }

    public void Dispose() => _client.Dispose();
}

public static class ObjectStorageFactory
{
    /// <summary>
    /// Chooses a storage provider from configuration. AEGISCORE_STORAGE_PROVIDER wins; failing
    /// that the cloud provider decides, because an installation on AWS usually wants S3. A
    /// provider this build cannot actually talk to throws here, at startup, naming what to do —
    /// the one thing it must never do is accept a file and drop it.
    /// </summary>
    public static IStorageService Create(IConfiguration configuration, ILoggerFactory loggers)
        => Choose(configuration) switch
        {
            "LOCAL" => new LocalFileStorageService(configuration, loggers.CreateLogger<LocalFileStorageService>()),
            _ => new S3CompatibleStorageService(configuration, loggers.CreateLogger<S3CompatibleStorageService>())
        };

    /// <summary>Throws if configuration names a provider this build cannot actually store files with.</summary>
    public static void Validate(IConfiguration configuration) => Choose(configuration);

    private static string Choose(IConfiguration configuration)
    {
        var storage = configuration["AEGISCORE_STORAGE_PROVIDER"];
        var cloud = configuration["AEGISCORE_CLOUD_PROVIDER"] ?? "ONPREMISE";
        var choice = (string.IsNullOrWhiteSpace(storage) ? cloud : storage).Trim().ToUpperInvariant();

        return choice switch
        {
            "LOCAL" or "FILE" or "FILESYSTEM" or "DISK" or "ONPREMISE" or "ONPREM" => "LOCAL",
            "S3" or "AWS" or "R2" or "CLOUDFLARE" or "MINIO" or "WASABI" or "CEPH" => "S3",
            "AZURE" => throw new InvalidOperationException(
                "AEGISCORE_STORAGE_PROVIDER=AZURE is not supported by this build: no Azure Blob client is compiled in, and accepting uploads it cannot store would lose them. Use AEGISCORE_STORAGE_PROVIDER=local with a mounted volume, or point AEGISCORE_STORAGE_PROVIDER=s3 at any S3-compatible endpoint."),
            "GCP" or "GCS" or "GOOGLE" => throw new InvalidOperationException(
                "AEGISCORE_STORAGE_PROVIDER=GCP is not supported by this build: no Cloud Storage client is compiled in, and accepting uploads it cannot store would lose them. Use AEGISCORE_STORAGE_PROVIDER=local with a mounted volume, or Cloud Storage's S3-compatible endpoint with AEGISCORE_STORAGE_PROVIDER=s3."),
            _ => throw new InvalidOperationException(
                $"AEGISCORE_STORAGE_PROVIDER='{choice}' is not a storage provider this build knows. Use 'local' (a mounted volume) or 's3' (S3, Cloudflare R2, MinIO or another S3-compatible endpoint).")
        };
    }
}
