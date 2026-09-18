using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Deployment;

public class StorageHealthCheck : IHealthCheck
{
    private readonly IStorageService _storage;
    private readonly ILogger<StorageHealthCheck> _logger;
    public StorageHealthCheck(IStorageService storage, ILogger<StorageHealthCheck> logger) { _storage = storage; _logger = logger; }
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        // The check writes, reads back and compares. Writing alone proved nothing: the storage
        // providers this replaced accepted every upload and kept none of them, and this check
        // reported healthy throughout.
        var testKey = $"health-check/{Guid.NewGuid():N}";
        var probe = Guid.NewGuid().ToByteArray();
        try
        {
            await _storage.CreateBucketIfNotExistsAsync("aegis-health-check", ct);
            await _storage.UploadAsync("aegis-health-check", testKey, new MemoryStream(probe), "application/octet-stream", cancellationToken: ct);

            await using var readBack = await _storage.DownloadAsync("aegis-health-check", testKey, ct);
            using var buffer = new MemoryStream();
            await readBack.CopyToAsync(buffer, ct);

            if (!buffer.ToArray().SequenceEqual(probe))
                return HealthCheckResult.Unhealthy($"Storage accepted a {probe.Length}-byte test object but returned {buffer.Length} bytes. Files written here are being lost — check the storage provider, its credentials and, for local storage, that the volume is mounted and writable.");

            return HealthCheckResult.Healthy($"Storage round trip OK via {_storage.GetType().Name}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage health check failed");
            return HealthCheckResult.Unhealthy($"Storage is not usable: {ex.Message}", ex);
        }
        finally
        {
            try { await _storage.DeleteAsync("aegis-health-check", testKey, ct); } catch { /* the probe is disposable */ }
        }
    }
}

public class LicenseHealthCheck : IHealthCheck
{
    private readonly ILicenseRepository _licenseRepo;
    private readonly ILogger<LicenseHealthCheck> _logger;
    public LicenseHealthCheck(ILicenseRepository licenseRepo, ILogger<LicenseHealthCheck> logger) { _licenseRepo = licenseRepo; _logger = logger; }
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var licenses = await _licenseRepo.GetByStatusAsync(LicenseStatus.Active, ct);
            if (licenses.Count == 0) return HealthCheckResult.Unhealthy("No active license found");
            var lic = licenses.First();
            if (lic.ExpiresAt.HasValue && lic.ExpiresAt.Value < DateTime.UtcNow.AddDays(30))
                return HealthCheckResult.Degraded($"License expires in {(lic.ExpiresAt.Value - DateTime.UtcNow).Days} days");
            return HealthCheckResult.Healthy($"License valid — {lic.Edition}, {(lic.ExpiresAt.HasValue ? $"expires {lic.ExpiresAt.Value:yyyy-MM-dd}" : "perpetual")}");
        }
        catch (Exception ex) { _logger.LogWarning(ex, "License health check failed"); return HealthCheckResult.Unhealthy("License check failed", ex); }
    }
}

public class TunnelHealthCheck : IHealthCheck
{
    private readonly ILogger<TunnelHealthCheck> _logger;
    public TunnelHealthCheck(ILogger<TunnelHealthCheck> logger) { _logger = logger; }
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        var tunnelId = Environment.GetEnvironmentVariable("CLOUDFLARE_TUNNEL_ID");
        if (string.IsNullOrEmpty(tunnelId)) return HealthCheckResult.Degraded("Cloudflare tunnel not configured");
        return HealthCheckResult.Healthy($"Tunnel ID: {tunnelId}");
    }
}
