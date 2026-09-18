using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Application.Admissions;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Admissions;

/// <summary>Applicant JWT audience. Kept distinct from the staff/student audience (<c>aegiscore-api</c>).</summary>
public static class ApplicantAuth
{
    public const string Audience = "applicant";
    public const string Scheme = "Applicant";
    public const string Issuer = "https://aegiscore.pk";
}

public sealed class ApplicantTenantResolver(IConfiguration configuration) : IApplicantTenantResolver
{
    public Guid ResolveTenantId()
    {
        var raw = configuration["TENANT_ID"] ?? configuration["Tenant:Id"];
        return Guid.TryParse(raw, out var id) && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("TENANT_ID is not configured for the applicant portal.");
    }
}

public sealed class ApplicantIdGenerator(AegisDbContext db) : IApplicantIdGenerator
{
    // Crockford-style: no vowels / ambiguous characters, matches the brief's NANOID8 examples.
    private const string Alphabet = "0123456789ABCDEFGHJKLMNPQRSTVWXYZ";

    public async Task<string> GenerateAsync(AdmissionIntake intake, DegreeProgram program, CancellationToken ct = default)
    {
        var code = (program.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            throw new InvalidOperationException($"Program '{program.Name}' has no admission short code configured.");
        }

        var prefix = $"APP-{intake.Year.ToString(CultureInfo.InvariantCulture)}{intake.SemesterCode}-{code}-";

        for (var attempt = 0; attempt < 12; attempt++)
        {
            var candidate = prefix + NanoidDotNet.Nanoid.Generate(Alphabet, 8);
            var exists = await db.ApplicantProfiles.IgnoreQueryFilters().AnyAsync(p => p.ApplicantId == candidate, ct);
            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not allocate a unique Applicant ID after repeated attempts.");
    }
}

public sealed class ApplicantOtpService(IConnectionMultiplexer redis, ILogger<ApplicantOtpService> logger) : IApplicantOtpService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private const int MaxAttempts = 5;

    private static string Key(string email) => $"aegiscore:applicant:otp:{email.Trim().ToLowerInvariant()}";

    public async Task<string> IssueAsync(string email, CancellationToken ct = default)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var payload = JsonSerializer.Serialize(new OtpEntry(code, 0));
        await redis.GetDatabase().StringSetAsync(Key(email), payload, Ttl);
        // Delivery is performed by the caller through INotificationService so this stays transport-agnostic.
        logger.LogInformation("Issued applicant OTP for {Email}", email);
        return code;
    }

    public async Task<bool> VerifyAsync(string email, string code, CancellationToken ct = default)
    {
        var dbi = redis.GetDatabase();
        var key = Key(email);
        var raw = await dbi.StringGetAsync(key);
        if (raw.IsNullOrEmpty)
        {
            return false;
        }

        var entry = JsonSerializer.Deserialize<OtpEntry>((string)raw!)!;
        if (entry.Attempts >= MaxAttempts)
        {
            await dbi.KeyDeleteAsync(key);
            return false;
        }

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(entry.Code), Encoding.UTF8.GetBytes(code.Trim())))
        {
            await dbi.StringSetAsync(key, JsonSerializer.Serialize(entry with { Attempts = entry.Attempts + 1 }), Ttl);
            return false;
        }

        await dbi.KeyDeleteAsync(key);
        return true;
    }

    private sealed record OtpEntry(string Code, int Attempts);
}

public sealed class ApplicantTokenService(IConfiguration configuration, IConnectionMultiplexer redis) : IApplicantTokenService
{
    private readonly int _lifetimeSeconds = int.TryParse(configuration["APPLICANT_SESSION_LIFETIME"], out var s) && s is >= 300 and <= 86_400
        ? s
        : 3600;

    private static string RevokeKey(Guid id) => $"aegiscore:applicant:revoked:{id}";

    public string IssueToken(ApplicantProfile profile)
    {
        var (key, alg) = ResolveSigningKey(configuration);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, profile.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("applicant_id", profile.ApplicantId),
            new("tenant_id", profile.TenantId.ToString()),
            new(ClaimTypes.Email, profile.Email),
            new("scope", "applicant")
        };

        var token = new JwtSecurityToken(
            issuer: ApplicantAuth.Issuer,
            audience: ApplicantAuth.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_lifetimeSeconds),
            signingCredentials: new SigningCredentials(key, alg));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Task RevokeAsync(Guid applicantProfileId, CancellationToken ct = default)
        => redis.GetDatabase().StringSetAsync(RevokeKey(applicantProfileId), "1", TimeSpan.FromSeconds(_lifetimeSeconds + 60));

    public async Task<bool> IsRevokedAsync(Guid applicantProfileId, CancellationToken ct = default)
        => await redis.GetDatabase().KeyExistsAsync(RevokeKey(applicantProfileId));

    /// <summary>Shared with the JWT bearer validation for the applicant scheme.</summary>
    public static (SecurityKey Key, string Algorithm) ResolveSigningKey(IConfiguration configuration)
    {
        var problem = AegisCore.Infrastructure.Security.JwtKeyMaterial.ProductionKeyProblem(configuration);
        if (problem is not null) throw new InvalidOperationException(problem);
        var signingKey = AegisCore.Infrastructure.Security.JwtKeyMaterial.SigningKey(configuration);
        if (signingKey is not null) return (signingKey, SecurityAlgorithms.RsaSha256);

        var secret = configuration["AEGISCORE_JWT_SECRET"] ?? configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Applicant token signing key is not configured.");
        return (new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);
    }

    public static IReadOnlyList<SecurityKey> ResolveValidationKeys(IConfiguration configuration)
    {
        var problem = AegisCore.Infrastructure.Security.JwtKeyMaterial.ProductionKeyProblem(configuration);
        if (problem is not null) throw new InvalidOperationException(problem);
        var keys = AegisCore.Infrastructure.Security.JwtKeyMaterial.ValidationKeys(configuration);
        if (keys.Count > 0) return keys;

        var secret = configuration["AEGISCORE_JWT_SECRET"] ?? configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Applicant token validation key is not configured.");
        return [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))];
    }
}

public sealed class ClipPhotoValidationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ClipPhotoValidationService> logger) : IPhotoValidationService
{
    public async Task<PhotoValidationResult> ValidateAsync(Stream image, string fileName, string contentType, CancellationToken ct = default)
    {
        var baseUrl = configuration["CLIP_SERVICE_URL"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("CLIP_SERVICE_URL not configured — applicant photo routed to manual review.");
            return FailOpen("Photo verification service is not configured");
        }

        try
        {
            using var client = httpClientFactory.CreateClient("clip");
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(20);

            using var form = new MultipartFormDataContent();
            using var buffer = new MemoryStream();
            await image.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            var fileContent = new StreamContent(buffer);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType);
            form.Add(fileContent, "image", string.IsNullOrWhiteSpace(fileName) ? "photo.jpg" : fileName);

            using var response = await client.PostAsync("validate-photo", form, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("CLIP service returned {Status} — applicant photo routed to manual review.", (int)response.StatusCode);
                return FailOpen("Photo verification service is unavailable");
            }

            var body = await response.Content.ReadFromJsonAsync<ClipResponse>(cancellationToken: ct);
            if (body is null)
            {
                return FailOpen("Photo verification service returned no result");
            }

            return new PhotoValidationResult(
                Passed: body.Passed,
                Confidence: body.Confidence,
                Reason: body.Passed ? string.Empty : (string.IsNullOrWhiteSpace(body.Reason) ? "Filtered or non-compliant photo detected" : body.Reason),
                RequiresManualReview: false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogError(ex, "CLIP service call failed — applicant photo routed to manual review (fail-open).");
            return FailOpen("Photo verification service is unreachable");
        }
    }

    private static PhotoValidationResult FailOpen(string reason)
        => new(Passed: true, Confidence: null, Reason: reason + " — flagged for manual review", RequiresManualReview: true);

    private sealed record ClipResponse(bool Passed, double Confidence, string? Reason);
}
