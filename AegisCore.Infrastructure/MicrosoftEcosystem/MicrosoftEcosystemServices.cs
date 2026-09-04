using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AegisCore.Application.MicrosoftEcosystem;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;

namespace AegisCore.Infrastructure.MicrosoftEcosystem;

public class TeamsProvisioningService : ITeamsProvisioningService
{
    private readonly AegisDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<TeamsProvisioningService> _logger;

    public TeamsProvisioningService(AegisDbContext db, IHttpClientFactory http, IConfiguration config, ILogger<TeamsProvisioningService> logger)
    {
        _db = db;
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<TeamsSyncResult> ProvisionClassAsync(Guid timetableEntryId, CancellationToken ct = default)
    {
        var log = new TeamsSyncLog { Id = Guid.NewGuid(), TimetableEntryId = timetableEntryId };
        _db.TeamsSyncLogs.Add(log);
        try
        {
            var entry = await _db.Timetables.Include(t => t.Course).Include(t => t.Faculty).FirstAsync(t => t.Id == timetableEntryId, ct);
            var token = _config["MicrosoftGraph:AccessToken"];
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Microsoft Graph access token is not configured.");

            var client = _http.CreateClient("graph");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await client.PostAsJsonAsync("education/classes", new
            {
                displayName = $"{entry.Course.Code ?? entry.Course.Title} - {entry.Faculty.FirstName} {entry.Faculty.LastName}",
                courseNumber = entry.Course.Code,
                externalId = timetableEntryId.ToString(),
                externalSource = "sis"
            }, ct);
            response.EnsureSuccessStatusCode();
            var graphClass = await response.Content.ReadFromJsonAsync<GraphClassResponse>(cancellationToken: ct);

            log.TeamsTeamId = graphClass?.Id;
            log.SyncStatus = "success";
            log.SyncedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            log.SyncStatus = "failed";
            log.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Teams provisioning failed for timetable entry {TimetableEntryId}", timetableEntryId);
        }
        await _db.SaveChangesAsync(ct);
        return new TeamsSyncResult(log.Id, log.SyncStatus, log.TeamsTeamId, log.ErrorMessage);
    }

    private sealed record GraphClassResponse([property: JsonPropertyName("id")] string? Id);
}

public class MoodleLmsService : IMoodleLmsService
{
    private readonly AegisDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<MoodleLmsService> _logger;

    public MoodleLmsService(AegisDbContext db, IHttpClientFactory http, IConfiguration config, ILogger<MoodleLmsService> logger)
    {
        _db = db;
        _http = http;
        _config = config;
        _logger = logger;
    }

    public Task<LmsSyncResult> SyncRosterAsync(Guid courseId, CancellationToken ct = default)
        => SyncMoodleAsync(courseId, "enrollment", "core_enrol_get_enrolled_users", ct);

    public Task<LmsSyncResult> SyncGradePassbackAsync(Guid courseId, CancellationToken ct = default)
        => SyncMoodleAsync(courseId, "grades", "gradereport_user_get_grade_items", ct);

    private async Task<LmsSyncResult> SyncMoodleAsync(Guid courseId, string syncType, string function, CancellationToken ct)
    {
        var log = new LmsSyncLog { Id = Guid.NewGuid(), CourseId = courseId, SyncType = syncType };
        _db.LmsSyncLogs.Add(log);
        try
        {
            var baseUrl = _config["Moodle:BaseUrl"];
            var token = _config["Moodle:Token"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Moodle base URL/token is not configured.");

            _ = await _db.Courses.FirstAsync(c => c.Id == courseId, ct);
            var response = await _http.CreateClient("moodle").GetAsync($"{baseUrl.TrimEnd('/')}/webservice/rest/server.php?wstoken={Uri.EscapeDataString(token)}&wsfunction={function}&moodlewsrestformat=json&courseid={courseId}", ct);
            response.EnsureSuccessStatusCode();
            log.SyncStatus = "success";
            log.SyncedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            log.SyncStatus = "failed";
            log.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Moodle {SyncType} sync failed for course {CourseId}", syncType, courseId);
        }
        await _db.SaveChangesAsync(ct);
        return new LmsSyncResult(log.Id, log.SyncStatus, syncType, log.ErrorMessage);
    }
}

public class PowerBiEmbeddingService : IPowerBiEmbeddingService
{
    private readonly AegisDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PowerBiEmbeddingService> _logger;

    private static readonly IAsyncPolicy AzureAdPolicy = Policy
        .WrapAsync(
            Policy.Handle<Exception>()
                .AdvancedCircuitBreakerAsync(1.0, TimeSpan.FromSeconds(30), 5, TimeSpan.FromSeconds(60)),
            Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1))),
            Policy.TimeoutAsync(TimeSpan.FromSeconds(10))
        );

    public PowerBiEmbeddingService(AegisDbContext db, IConfiguration config, ILogger<PowerBiEmbeddingService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<PowerBiEmbedConfig> GetEmbedConfigAsync(Guid userId, string role, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A valid user id is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("A Power BI role is required.", nameof(role));
        var normalizedRole = role.Trim().ToLowerInvariant();
        if (!PowerBiRoles.Allowed.Contains(normalizedRole)) throw new ArgumentException("Unsupported Power BI role.", nameof(role));
        var report = await _db.PowerBiReports.AsNoTracking().FirstAsync(r => r.TargetRole.ToLower() == normalizedRole, ct);
        if (!Uri.TryCreate(report.EmbedUrl, UriKind.Absolute, out var embedUri) || embedUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Power BI report EmbedUrl must use HTTPS.");
        var minutes = int.TryParse(_config["PowerBi:TokenMinutes"], out var m) && m is >= 1 and <= 60 ? m : 30;

        try
        {
            var staticToken = _config["PowerBi:EmbedToken"];
            if (!string.IsNullOrWhiteSpace(staticToken))
            {
                return new PowerBiEmbedConfig(report.PowerBiReportId, report.PowerBiWorkspaceId, report.EmbedUrl, staticToken, DateTimeOffset.UtcNow.AddMinutes(minutes));
            }
        }
        catch { }

        try
        {
            var tenantId = _config["Azure:TenantId"] ?? _config["AZURE_TENANT_ID"];
            var clientId = _config["Azure:ClientId"] ?? _config["AZURE_CLIENT_ID"];
            var clientSecret = _config["Azure:ClientSecret"] ?? _config["AZURE_CLIENT_SECRET"];
            var authority = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("Azure AD credentials are not configured for Power BI token acquisition.");

            AzureAdTokenResponse? tokenResponse = null;
            await AzureAdPolicy.ExecuteAsync(async innerCt =>
            {
                using var http = new HttpClient();
                using var body = new FormUrlEncodedContent([
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("scope", "https://analysis.windows.net/powerbi/api/.default")
                ]);

                var response = await http.PostAsync(authority, body, innerCt);
                response.EnsureSuccessStatusCode();
                var tr = await response.Content.ReadFromJsonAsync<AzureAdTokenResponse>(cancellationToken: innerCt);
                if (tr?.AccessToken is null)
                    throw new InvalidOperationException("Failed to acquire Azure AD token for Power BI.");
                tokenResponse = tr;
            }, ct);

            if (tokenResponse?.AccessToken is null)
                throw new InvalidOperationException("Failed to acquire Azure AD token for Power BI.");

            return new PowerBiEmbedConfig(report.PowerBiReportId, report.PowerBiWorkspaceId, report.EmbedUrl, tokenResponse.AccessToken, DateTimeOffset.UtcNow.AddMinutes(minutes));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Power BI Azure AD token acquisition failed, falling back to static config if available.");
            var fallbackToken = _config["PowerBi:EmbedToken"];
            if (!string.IsNullOrWhiteSpace(fallbackToken))
                return new PowerBiEmbedConfig(report.PowerBiReportId, report.PowerBiWorkspaceId, report.EmbedUrl, fallbackToken, DateTimeOffset.UtcNow.AddMinutes(minutes));
            throw new InvalidOperationException("Power BI embed token is not available. Configure Azure:TenantId, Azure:ClientId, and Azure:ClientSecret or PowerBi:EmbedToken.", ex);
        }
    }

    private sealed record AzureAdTokenResponse(string AccessToken, string TokenType, int ExpiresIn);
}
