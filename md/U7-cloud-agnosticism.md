# AegisCore — Cloud Agnosticism Mandate (U7)

## Role
You are a senior .NET 10 / C# 14 architect ensuring AegisCore is **100% cloud-agnostic**. Zero vendor lock-in. Pakistani universities must be able to run this on Azure, AWS, GCP, local servers, private data centers, or any cloud provider without changing a single line of application code.

This is a **compliance audit + implementation spec** combined.

---

## The Promise to Universities

**We guarantee:**
- ✅ Your data never leaves your infrastructure
- ✅ No automatic backups to US/Europe servers
- ✅ No phone-home analytics (except license heartbeat, which you control)
- ✅ Run on Azure/AWS/GCP/local/on-prem — same code, zero changes
- ✅ Switch cloud providers mid-production without re-architecting
- ✅ Compliance with Pakistani data residency laws

---

## Part A — AUDIT: Find All Cloud Vendor Lock-in

### 1. NuGet Package Audit

**SCAN:**
```bash
dotnet list package --outdated
grep -r "Azure\." **/*.csproj
grep -r "AWSSDK\." **/*.csproj
grep -r "Google\.Cloud\." **/*.csproj
grep -r "Stripe\|Twilio\|SendGrid" **/*.csproj
```

**FORBIDDEN (unless abstracted):**
- `Azure.Storage.Blobs` — must be behind `IStorageProvider`
- `Azure.Identity` — must be behind `IAuthProvider`
- `Azure.Messaging.ServiceBus` — must be behind `IMessageQueueProvider`
- `Azure.Extensions.AspNetCore.Configuration.Secrets` — must be behind `ISecretProvider`
- `AWSSDK.S3` — must be behind `IStorageProvider`
- `AWSSDK.Sns` — must be behind `IMessageQueueProvider`
- `Google.Cloud.Storage.V1` — must be behind `IStorageProvider`
- `Google.Cloud.SecretManager.V1` — must be behind `ISecretProvider`
- `Microsoft.EntityFrameworkCore.SqlServer` — OK (EF abstraction), but must also support PostgreSQL
- `Google.Cloud.Firestore` — FORBIDDEN — no NoSQL vendor lock-in

**ALLOWED:**
- `Microsoft.EntityFrameworkCore` (abstraction)
- `Npgsql` (PostgreSQL provider — open-source, runs anywhere)
- `StackExchange.Redis` (Redis client — runs anywhere)
- `Minio` (S3-compatible, open-source)
- `System.Net.Http` (standard library)

### 2. Configuration File Audit

**Check:**
```bash
grep -r "AzureWebJobsStorage" **/*.json
grep -r "DefaultAzureCredential" **/*.cs
grep -r "https://.*vault.azure.com" **/*.json
grep -r "https://.*kms.*.amazonaws.com" **/*.cs
grep -r "https://.*secretmanager.googleapis.com" **/*.cs
grep -r "app-insights\|application-insights" **/*.json
```

**Rule:** No hardcoded cloud-vendor URLs. Everything via `appsettings.json` + environment variables.

### 3. Code Audit — Direct SDK Calls

**FORBIDDEN patterns:**
```csharp
// ❌ FORBIDDEN
var client = new BlobContainerClient(uri);  // Azure SDK directly
var s3Client = new AmazonS3Client();        // AWS SDK directly
var storageClient = new StorageClient();    // GCP SDK directly

// ✅ ALLOWED
var storage = _serviceProvider.GetRequiredService<IStorageProvider>();
await storage.UploadAsync("file.pdf", stream);
```

**Scan for:**
```bash
grep -r "BlobContainerClient\|BlobClient\|CloudBlockBlob" **/*.cs
grep -r "AmazonS3Client\|S3Client\|PutObjectAsync" **/*.cs
grep -r "StorageClient\|BucketClient" **/*.cs
grep -r "CosmosClient\|DocumentClient" **/*.cs
grep -r "ServiceBusClient\|QueueClient" **/*.cs
grep -r "SecretClient\|KeyVaultClient" **/*.cs
```

Any matches = **blocker**.

### 4. Connection String Audit

**FORBIDDEN patterns:**
```
DefaultEndpointsProtocol=https;AccountName=xxx;AccountKey=xxx;EndpointSuffix=core.windows.net
arn:aws:s3:::bucket-name
gs://bucket-name
```

**REQUIRED:**
```
Host=localhost;Database=aegiscore;Username=postgres;Password=***;Port=5432
redis://localhost:6379
s3://bucket-name (with provider-agnostic client)
file:///mnt/storage/  (for local)
```

### 5. Environment Variable Audit

Every environment-specific setting must be injectable. Check for:

```bash
grep -r "Environment\.GetEnvironmentVariable" **/*.cs
```

Examples of what MUST be env vars:
- `AEGISCORE_DB_CONNECTION` — database connection string
- `AEGISCORE_REDIS_CONNECTION` — cache connection string
- `AEGISCORE_STORAGE_PROVIDER` — local | s3 | azure | sftp | gcs
- `AEGISCORE_STORAGE_CONNECTION` — provider-specific connection
- `AEGISCORE_ENCRYPTION_KEY` — AES-256 key for data at rest
- `AEGISCORE_HEARTBEAT_ENABLED` — true/false for license
- `AEGISCORE_ANALYTICS_ENABLED` — true/false

**Rule:** No hardcoded URLs, credentials, or cloud-specific paths.

### 6. Infrastructure-as-Code Audit

**Check Dockerfile:**
```dockerfile
# ❌ FORBIDDEN
RUN apt-get install azure-cli
RUN apt-get install aws-cli

# ✅ ALLOWED
RUN apt-get install postgresql-client
RUN apt-get install redis-tools
```

**Check docker-compose.yml:**
```yaml
services:
  postgres:
    image: postgres:16-alpine  # ✅ Cloud-agnostic
  redis:
    image: redis:7-alpine      # ✅ Cloud-agnostic
  # ❌ NO cloud-vendor-specific services
```

---

## Part B — IMPLEMENTATION: Enforce Cloud Agnosticism

### 1. Storage Abstraction (Complete)

Every file operation MUST go through `IStorageProvider`.

```csharp
public interface IStorageProvider
{
    /// <summary>
    /// Upload file to storage (local/S3/Azure/SFTP — transparent to caller)
    /// </summary>
    Task<string> UploadAsync(
        string path,
        Stream content,
        string contentType = "application/octet-stream",
        CancellationToken ct = default
    );

    /// <summary>
    /// Download file from storage
    /// </summary>
    Task<Stream> DownloadAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Delete file from storage
    /// </summary>
    Task DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Check if file exists
    /// </summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// List files with prefix (for directory browsing)
    /// </summary>
    Task<IReadOnlyList<string>> ListAsync(
        string prefix,
        CancellationToken ct = default
    );

    /// <summary>
    /// Generate signed URL (if supported by provider)
    /// </summary>
    Task<string> GenerateSignedUrlAsync(
        string path,
        TimeSpan expiresIn,
        CancellationToken ct = default
    );
}
```

**Implementations Required:**

#### LocalDiskStorageProvider
```csharp
public class LocalDiskStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    
    public LocalDiskStorageProvider(IConfiguration config)
    {
        _basePath = config["Storage:Local:BasePath"] ?? "/app/storage";
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(
        string path, Stream content, string contentType = "application/octet-stream", CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path);
        var directory = Path.GetDirectoryName(fullPath);
        Directory.CreateDirectory(directory!);

        using (var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true))
        {
            await content.CopyToAsync(fileStream, 81920, ct);
        }

        return path;
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");

        return new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, path);
        return File.Exists(fullPath) || Directory.Exists(fullPath);
    }

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, prefix);
        if (!Directory.Exists(fullPath))
            return new List<string>();

        return Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories)
            .Select(f => f.Replace(_basePath, "").TrimStart(Path.DirectorySeparatorChar))
            .ToList();
    }

    public Task<string> GenerateSignedUrlAsync(
        string path, TimeSpan expiresIn, CancellationToken ct = default)
    {
        // Local disk doesn't have signed URLs — return direct path
        return Task.FromResult(path);
    }
}
```

#### S3StorageProvider (MinIO + AWS S3 Compatible)
```csharp
public class S3StorageProvider : IStorageProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3StorageProvider(IConfiguration config)
    {
        _bucketName = config["Storage:S3:BucketName"]
            ?? throw new InvalidOperationException("S3 bucket name not configured");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = config["Storage:S3:Endpoint"], // Works with MinIO, AWS, Wasabi, etc
            ForcePathStyle = true,
            SignatureVersion = "4"
        };

        _s3Client = new AmazonS3Client(
            config["Storage:S3:AccessKey"],
            config["Storage:S3:SecretKey"],
            s3Config
        );
    }

    public async Task<string> UploadAsync(
        string path, Stream content, string contentType = "application/octet-stream", CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = path,
            InputStream = content,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, ct);
        return path;
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken ct = default)
    {
        var response = await _s3Client.GetObjectAsync(_bucketName, path, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        await _s3Client.DeleteObjectAsync(_bucketName, path, ct);
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_bucketName, path, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default)
    {
        var request = new ListObjectsV2Request { BucketName = _bucketName, Prefix = prefix };
        var response = await _s3Client.ListObjectsV2Async(request, ct);

        return response.S3Objects.Select(obj => obj.Key).ToList();
    }

    public async Task<string> GenerateSignedUrlAsync(
        string path, TimeSpan expiresIn, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = path,
            Expires = DateTime.UtcNow.Add(expiresIn)
        };

        return _s3Client.GetPreSignedURL(request);
    }
}
```

#### SftpStorageProvider (On-Prem)
```csharp
public class SftpStorageProvider : IStorageProvider
{
    private readonly SftpClient _sftpClient;
    private readonly string _basePath;

    public SftpStorageProvider(IConfiguration config)
    {
        var host = config["Storage:Sftp:Host"];
        var port = int.Parse(config["Storage:Sftp:Port"] ?? "22");
        var username = config["Storage:Sftp:Username"];
        var password = config["Storage:Sftp:Password"];
        _basePath = config["Storage:Sftp:BasePath"] ?? "/home/storage";

        _sftpClient = new SftpClient(host, port, username, password);
        _sftpClient.Connect();
    }

    public async Task<string> UploadAsync(
        string path, Stream content, string contentType = "application/octet-stream", CancellationToken ct = default)
    {
        var fullPath = $"{_basePath}/{path}";
        _sftpClient.UploadFile(content, fullPath);
        return path;
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = $"{_basePath}/{path}";
        var stream = new MemoryStream();
        _sftpClient.DownloadFile(fullPath, stream);
        stream.Position = 0;
        return stream;
    }

    // ... other methods similar to above
}
```

**Registration in Program.cs:**
```csharp
builder.Services.AddScoped<IStorageProvider>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var provider = config["Storage:Provider"];

    return provider switch
    {
        "local" => new LocalDiskStorageProvider(config),
        "s3" => new S3StorageProvider(config),
        "sftp" => new SftpStorageProvider(config),
        _ => throw new InvalidOperationException($"Unknown storage provider: {provider}")
    };
});
```

### 2. Database Abstraction (Partial — EF Core Handles This)

EF Core supports multiple databases. Ensure:

```csharp
// ✅ Program.cs — Cloud agnostic
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString); // PostgreSQL (open-source, runs anywhere)
});
```

**Rule:** Use PostgreSQL (Npgsql provider). No SQL Server / Cosmos DB.

### 3. Secrets Management Abstraction

Never use Azure Key Vault directly.

```csharp
public interface ISecretProvider
{
    Task<string> GetSecretAsync(string secretName, CancellationToken ct = default);
    Task SetSecretAsync(string secretName, string secretValue, CancellationToken ct = default);
}

public class LocalSecretsProvider : ISecretProvider
{
    private readonly IConfiguration _config;

    public LocalSecretsProvider(IConfiguration config) => _config = config;

    public Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        var value = _config[$"Secrets:{secretName}"];
        if (string.IsNullOrEmpty(value))
            throw new KeyNotFoundException($"Secret not found: {secretName}");
        return Task.FromResult(value);
    }

    public Task SetSecretAsync(string secretName, string secretValue, CancellationToken ct = default)
    {
        // For local: just update appsettings, but in prod this is read-only
        return Task.CompletedTask;
    }
}

public class VaultSecretsProvider : ISecretProvider
{
    // IF university wants to use HashiCorp Vault (cloud-agnostic)
    private readonly IVaultClient _vault;

    public VaultSecretsProvider(IVaultClient vault) => _vault = vault;

    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        var secret = await _vault.V1.Secrets.KeyValue.V2.ReadSecretAsync(
            $"aegiscore/{secretName}",
            cancellationToken: ct
        );
        return secret.Data.Data["value"].ToString();
    }

    public async Task SetSecretAsync(string secretName, string secretValue, CancellationToken ct = default)
    {
        await _vault.V1.Secrets.KeyValue.V2.WriteSecretAsync(
            $"aegiscore/{secretName}",
            new Dictionary<string, object> { { "value", secretValue } },
            cancellationToken: ct
        );
    }
}
```

**Registration:**
```csharp
var secretProvider = builder.Configuration["SecretsProvider"];
builder.Services.AddScoped<ISecretProvider>(sp =>
    secretProvider switch
    {
        "local" => new LocalSecretsProvider(sp.GetRequiredService<IConfiguration>()),
        "vault" => new VaultSecretsProvider(sp.GetRequiredService<IVaultClient>()),
        _ => throw new InvalidOperationException($"Unknown secrets provider: {secretProvider}")
    }
);
```

### 4. Message Queue Abstraction (Optional — for async jobs)

If you use background jobs:

```csharp
public interface IMessageQueueProvider
{
    Task EnqueueAsync(string queueName, string message, CancellationToken ct = default);
    Task<string> DequeueAsync(string queueName, TimeSpan timeout, CancellationToken ct = default);
}

public class InMemoryQueueProvider : IMessageQueueProvider
{
    // Simple in-memory queue for local dev
}

public class RedisQueueProvider : IMessageQueueProvider
{
    // Redis-backed queue (works on any cloud)
}
```

### 5. Encryption at Rest (Cloud-Agnostic)

```csharp
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(IConfiguration config)
    {
        var keyBase64 = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption key not configured");
        _key = Convert.FromBase64String(keyBase64);
    }

    public string Encrypt(string plaintext)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            var iv = aes.IV;

            using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
            using (var ms = new MemoryStream())
            {
                ms.Write(iv, 0, iv.Length); // Prepend IV

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plaintext);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public string Decrypt(string ciphertext)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _key;

            var buffer = Convert.FromBase64String(ciphertext);
            var iv = new byte[aes.IV.Length];
            Array.Copy(buffer, 0, iv, 0, iv.Length);

            using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
            using (var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
```

---

## Part C — VALIDATION: Prove It Works Everywhere

### 1. Environment Variable Completeness

```bash
# List all required env vars
AEGISCORE_DB_PROVIDER=postgresql
AEGISCORE_DB_CONNECTION=Host=postgres;Database=aegiscore;Username=postgres;Password=***;Port=5432
AEGISCORE_REDIS_CONNECTION=redis://redis:6379
AEGISCORE_STORAGE_PROVIDER=local|s3|sftp
AEGISCORE_STORAGE_CONNECTION=<provider-specific>
AEGISCORE_ENCRYPTION_KEY=<base64-encoded-256-bit-key>
AEGISCORE_LICENSE_SERVER_URL=https://license.apex-systems.com
AEGISCORE_HEARTBEAT_ENABLED=true
```

**Test:** Run app with ONLY env vars (no appsettings.json hardcoding).

### 2. Multi-Cloud Docker Compose Tests

#### Test 1: Local Disk Storage
```yaml
version: '3.9'
services:
  app:
    build: .
    environment:
      AEGISCORE_STORAGE_PROVIDER: local
      AEGISCORE_STORAGE_CONNECTION: /app/storage
      AEGISCORE_DB_CONNECTION: Host=postgres;Database=aegiscore;...
  postgres:
    image: postgres:16
```

#### Test 2: MinIO (S3-Compatible, On-Prem)
```yaml
services:
  minio:
    image: minio/minio
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
  app:
    environment:
      AEGISCORE_STORAGE_PROVIDER: s3
      AEGISCORE_STORAGE_CONNECTION: Endpoint=http://minio:9000;AccessKey=minioadmin;SecretKey=minioadmin;BucketName=aegiscore
```

#### Test 3: SFTP (Private Data Center)
```yaml
services:
  app:
    environment:
      AEGISCORE_STORAGE_PROVIDER: sftp
      AEGISCORE_STORAGE_CONNECTION: Host=sftp.university.com;Username=storage;Password=***;BasePath=/data/aegiscore
```

### 3. Deployment Checklist

Before sending to ANY university:

```
CLOUD AGNOSTICISM SIGN-OFF
==========================

Database:
  ☐ PostgreSQL only (no SQL Server, no Cosmos, no Firestore)
  ☐ Connection string injected via env var
  ☐ No hardcoded connection strings in code

Storage:
  ☐ All file ops go through IStorageProvider
  ☐ No direct Azure.Storage.Blobs calls
  ☐ No direct S3 calls outside S3StorageProvider
  ☐ LocalDiskStorageProvider works offline
  ☐ S3StorageProvider tested with MinIO + AWS
  ☐ SFTP provider tested with on-prem server

Secrets:
  ☐ ISecretProvider abstracts secrets management
  ☐ No hardcoded API keys / credentials
  ☐ appsettings.json has no production secrets
  ☐ All secrets injectable via env vars

Configuration:
  ☐ Zero cloud-vendor URLs in code
  ☐ All URLs in appsettings.json or env vars
  ☐ No Application Insights / DataDog hardcoding
  ☐ No auto-backup to US servers

Infrastructure:
  ☐ Dockerfile runs on any Docker host
  ☐ No cloud CLI tools (az, aws, gcloud)
  ☐ docker-compose.yml is cloud-vendor neutral
  ☐ PostgreSQL, Redis standard images
  ☐ Tested on local Docker, AWS ECS, Azure AKS, GKE

Code Review:
  ☐ No grep -r "Azure\." in critical paths
  ☐ No grep -r "AWSSDK\." outside abstractions
  ☐ No grep -r "Google\.Cloud\." in business logic
  ☐ All cloud SDK calls behind interfaces

Testing:
  ☐ Unit tests mock IStorageProvider
  ☐ Integration tests run on local Docker
  ☐ E2E tests run on local disk, MinIO, SFTP
  ☐ No cloud account credentials in test code

Documentation:
  ☐ .env.example lists all required vars
  ☐ README explains local/on-prem/cloud deployments
  ☐ Deployment guide for each cloud + on-prem
  ☐ No mention of "must use Azure" or "must use AWS"

Deployment:
  ☐ Same Docker image runs on all platforms
  ☐ Tested: local laptop, university server, AWS, GCP, Azure
  ☐ University can deploy independently (no Apex dependency)
  ☐ Data never leaves university's infrastructure
```

---

## Part D — The Pitch to Universities

When you show this to SSUET or any Pakistani university:

> "AegisCore is completely cloud-agnostic. Whether you run it locally on your servers, on AWS in an Indian data center, on Azure in Singapore, or on a private cloud — **the code is identical, zero changes**. You control your data. You choose your infrastructure. No vendor lock-in. No data leaving your jurisdiction unless YOU decide to."

**Proof:**
- Show docker-compose with local disk + PostgreSQL (works on their laptop)
- Show docker-compose with S3 MinIO (university's own MinIO server on-prem)
- Show Dockerfile (no cloud CLI tools)
- Show appsettings.json (only placeholders, all actual values from env vars)
- Run the app on 3 different environments in the meeting

---

## Implementation Checklist

When implementing cloud agnosticism:

- [ ] Create all 4 storage provider implementations (local, S3, Azure, SFTP)
- [ ] Audit codebase for vendor-locked dependencies
- [ ] Create ISecretProvider abstraction
- [ ] Update Program.cs to use DI for all abstractions
- [ ] Create `.env.example` with all required variables
- [ ] Create docker-compose files for local, S3, SFTP scenarios
- [ ] Unit test each storage provider
- [ ] Integration test full app flow on 3+ environments
- [ ] Documentation: "Deployment Guide for [Local / S3 / Azure / AWS / On-Prem]"
- [ ] Sign-off: security + architecture review

---

## Zero-Trust Guarantee Clause

Add this to your contract with universities:

> "AegisCore is designed with zero assumption of cloud-vendor infrastructure. All data encryption, backup, and redundancy is the university's responsibility. AegisCore Systems does not auto-backup, does not access university data, and does not transmit data outside university's chosen infrastructure. License heartbeat (08:00 AM daily, 10s timeout) is the only outbound connection, which can be disabled offline for air-gapped deployments."

This is your competitive advantage over Blackbaud / Ellucian / banner. They're cloud-only. You're **anywhere**.
