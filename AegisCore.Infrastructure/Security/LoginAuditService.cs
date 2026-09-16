using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Security;

public interface ILoginAuditService
{
    Task RecordAsync(LoginAuditRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnomalyAlert>> DetectAnomaliesAsync(LoginAuditRecord record, CancellationToken cancellationToken = default);
}

public interface IGeoIpService
{
    Task<GeoIpResult?> LookupAsync(string ipAddress, CancellationToken cancellationToken = default);
}

public record GeoIpResult(string Country, string City);

public record AnomalyAlert(string Type, string Severity, string Description, Guid? UserId);

public class GeoIpService : IGeoIpService
{
    private readonly ILogger<GeoIpService> _logger;
    public GeoIpService(ILogger<GeoIpService> logger) { _logger = logger; }

    public async Task<GeoIpResult?> LookupAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ipAddress)) return null;
        if (!IPAddress.TryParse(ipAddress, out var ip)) return null;
        if (IPAddress.IsLoopback(ip) || ip.IsPrivate()) return new GeoIpResult("Local", "Campus");

        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(3);
            var response = await http.GetFromJsonAsync<GeoIpApiResponse>(
                $"https://ipapi.co/{ipAddress}/json/", cancellationToken);
            if (response?.CountryName is not null)
                return new GeoIpResult(response.CountryName, response.City ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GeoIP lookup failed for {Ip}, using fallback", ipAddress);
        }

        _logger.LogDebug("GeoIP lookup for {Ip}", ipAddress);
        return new GeoIpResult("Unknown", "Unknown");
    }

    private sealed record GeoIpApiResponse(string? CountryName, string? City);
}

public class LoginAuditService : ILoginAuditService
{
    private readonly ILoginAuditRecordRepository _repo;
    private readonly IGeoIpService _geoIp;
    private readonly Channel<LoginAuditRecord> _archiveChannel;
    private readonly ILogger<LoginAuditService> _logger;

    public LoginAuditService(ILoginAuditRecordRepository repo, IGeoIpService geoIp, ILogger<LoginAuditService> logger)
    {
        _repo = repo; _geoIp = geoIp; _logger = logger;
        _archiveChannel = Channel.CreateUnbounded<LoginAuditRecord>();
    }

    public ChannelReader<LoginAuditRecord> ArchiveReader => _archiveChannel.Reader;

    public async Task RecordAsync(LoginAuditRecord record, CancellationToken ct = default)
    {
        var geo = await _geoIp.LookupAsync(record.IpAddress, ct);
        if (geo != null) { record.IpCountry = geo.Country; record.IpCity = geo.City; }

        if (!string.IsNullOrEmpty(record.UserAgent))
        {
            record.DeviceType = record.UserAgent.Contains("Mobile") ? DeviceType.Mobile
                : record.UserAgent.Contains("Tablet") ? DeviceType.Tablet : DeviceType.Desktop;
        }

        await _repo.AddAsync(record, ct);
        await _archiveChannel.Writer.WriteAsync(record, ct);
        _ = Task.Run(() => DetectAnomaliesAsync(record, ct), ct);

        _logger.LogInformation("Login audit: {Event} for {Email} from {Ip} ({Country})",
            record.EventType, record.UserEmail, record.IpAddress, record.IpCountry);
    }

    public async Task<IReadOnlyList<AnomalyAlert>> DetectAnomaliesAsync(LoginAuditRecord record, CancellationToken ct = default)
    {
        var alerts = new List<AnomalyAlert>();
        if (!record.UserId.HasValue || string.IsNullOrEmpty(record.IpAddress)) return alerts;

        var recentLogins = await _repo.GetByUserAsync(record.UserId.Value, DateTime.UtcNow.AddDays(-30), null, ct);
        var lastSuccessful = recentLogins.Where(r => r.IsSuccessful).Take(30).ToList();

        if (lastSuccessful.Count > 0 && record.IpCountry != "Local" && record.IpCountry != lastSuccessful[0].IpCountry)
            alerts.Add(new AnomalyAlert("NewCountryLogin", "Medium", $"Login from {record.IpCountry} while recent logins from {lastSuccessful[0].IpCountry}", record.UserId));

        if (record.EventType == LoginEvent.LoginFailed)
        {
            var recentFailures = recentLogins.Count(r => r.EventType == LoginEvent.LoginFailed && r.OccurredAt > DateTime.UtcNow.AddMinutes(-10));
            if (recentFailures >= 5)
                alerts.Add(new AnomalyAlert("BruteForce", "High", $"{recentFailures} failed logins in 10 minutes for {record.UserEmail}", record.UserId));
        }

        return alerts;
    }
}

public class LoginAuditArchiveWorker : BackgroundService
{
    private readonly LoginAuditService _auditService;
    private readonly IServiceProvider _sp;
    private readonly ILogger<LoginAuditArchiveWorker> _logger;

    public LoginAuditArchiveWorker(LoginAuditService auditService, IServiceProvider sp, ILogger<LoginAuditArchiveWorker> logger)
    { _auditService = auditService; _sp = sp; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var reader = _auditService.ArchiveReader;
        await foreach (var record in reader.ReadAllAsync(ct))
        {
            try
            {
                using var scope = _sp.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
                var key = $"login-audit/{record.TenantId}/{record.OccurredAt:yyyy/MM/dd/HH}/{record.AuditId}.ndjson";
                var json = JsonSerializer.Serialize(record);
                var bytes = Encoding.UTF8.GetBytes(json);
                await storage.UploadAsync("audit-logs", key, new MemoryStream(bytes), "application/x-ndjson", cancellationToken: ct);
                _logger.LogDebug("Archived audit record to R2: {Key}", key);
            }
            catch (Exception ex) { _logger.LogError(ex, "Failed to archive audit record {AuditId}", record.AuditId); }
        }
    }
}

public static class IPAddressExtensions
{
    public static bool IsPrivate(this IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return (bytes[0] == 10) || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || (bytes[0] == 192 && bytes[1] == 168) || IPAddress.IsLoopback(ip);
    }
}
