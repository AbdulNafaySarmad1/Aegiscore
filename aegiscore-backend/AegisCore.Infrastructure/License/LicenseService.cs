using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AegisCore.Application.Foundation;
using AegisCore.Application.Licensing;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Licensing;

public sealed class LicenseService(
    AegisDbContext db,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IAuditService audit,
    ILogger<LicenseService> logger) : ILicenseService
{
    public async Task<LicenseValidationResult> ValidateAsync(string licenseKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return Fail("License key is required.", LicenseValidationFailureReason.InvalidSignature);
        }

        var secret = configuration["License:HmacSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            return Fail("License verification secret is not configured.", LicenseValidationFailureReason.InvalidSignature);
        }

        JsonObject payload;
        try
        {
            payload = JsonNode.Parse(licenseKey)?.AsObject() ?? throw new JsonException();
            var signature = payload["signature"]?.GetValue<string>();
            payload.Remove("signature");

            if (string.IsNullOrWhiteSpace(signature) || !FixedEquals(signature, Sign(payload, secret)))
            {
                return Fail("License signature is invalid.", LicenseValidationFailureReason.InvalidSignature);
            }

            var info = new LicenseInfo(
                Guid.Parse(Required(payload, "university_id")),
                Required(payload, "university_name"),
                DateOnly.Parse(Required(payload, "issued_date")),
                DateOnly.Parse(Required(payload, "expiry_date")),
                int.Parse(Required(payload, "max_concurrent_users")),
                payload["licensed_modules"]?.AsArray().Select(v => v?.GetValue<string>() ?? string.Empty).Where(v => v.Length > 0).ToList() ?? [],
                Required(payload, "issued_by"));

            return info.ExpiryDate < DateOnly.FromDateTime(DateTime.UtcNow)
                ? new LicenseValidationResult(false, "License is expired.", info, LicenseValidationFailureReason.Expired)
                : new LicenseValidationResult(true, "License is valid.", info, null);
        }
        catch (JsonException)
        {
            return Fail("License key is not valid JSON.", LicenseValidationFailureReason.InvalidSignature);
        }
        catch (FormatException)
        {
            return Fail("License payload is malformed.", LicenseValidationFailureReason.InvalidSignature);
        }
        catch (InvalidOperationException)
        {
            return Fail("License payload is malformed.", LicenseValidationFailureReason.InvalidSignature);
        }
    }

    public async Task ActivateAsync(string licenseKey, Guid universityId, CancellationToken ct = default)
    {
        var validation = await ValidateAsync(licenseKey, ct);
        if (!validation.IsValid || validation.License is null)
        {
            throw new InvalidOperationException(validation.ValidationMessage);
        }

        if (validation.License.UniversityId != universityId)
        {
            throw new InvalidOperationException("License university does not match activation request.");
        }

        var state = await CurrentAsync(ct) ?? new LicenseState { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow };
        state.LicenseKey = licenseKey;
        state.UniversityId = validation.License.UniversityId;
        state.UniversityName = validation.License.UniversityName;
        state.Status = "active";
        state.LicenseJson = RedactSignature(licenseKey);
        state.ActivatedAt ??= DateTimeOffset.UtcNow;
        state.LockedAt = null;
        state.LockReason = null;
        state.UpdatedAt = DateTimeOffset.UtcNow;

        if (db.Entry(state).State == EntityState.Detached)
        {
            db.LicenseStates.Add(state);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<HeartbeatResult> SendHeartbeatAsync(CancellationToken ct = default)
    {
        var state = await CurrentAsync(ct);
        if (state is null)
        {
            return await FailHeartbeatAsync(null, "pending_activation", "License is not activated.", ct);
        }

        var endpoint = configuration["License:HeartbeatUrl"] ?? configuration["License:ServerUrl"];
        if (string.IsNullOrWhiteSpace(endpoint)
            || !Uri.TryCreate(endpoint, UriKind.Absolute, out var heartbeatUri)
            || heartbeatUri.Scheme != Uri.UriSchemeHttps)
        {
            return await FailHeartbeatAsync(state, "failed", "License heartbeat endpoint must be an absolute HTTPS URL.", ct);
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(int.TryParse(configuration["License:HeartbeatTimeoutSeconds"], out var seconds) ? seconds : 10));

            var client = httpClientFactory.CreateClient("license");
            if (configuration["License:HeartbeatServerToken"] is { Length: > 0 } token)
            {
                client.DefaultRequestHeaders.Remove("X-AegisCore-License-Token");
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-AegisCore-License-Token", token);
            }
            var response = await client.PostAsJsonAsync(heartbeatUri, new
            {
                university_id = state.UniversityId,
                university_name = state.UniversityName,
                current_user_count = await db.Users.CountAsync(ct),
                active_modules = JsonNode.Parse(state.LicenseJson)?["licensed_modules"],
                timestamp = DateTimeOffset.UtcNow
            }, timeout.Token);

            var body = await response.Content.ReadFromJsonAsync<HeartbeatResponse>(cancellationToken: timeout.Token)
                ?? new HeartbeatResponse("invalid", "Empty heartbeat response.");

            if (response.IsSuccessStatusCode && body.Status == "valid")
            {
                state.LastHeartbeatAt = DateTimeOffset.UtcNow;
                state.LastHeartbeatStatus = "success";
                state.FailedHeartbeatCount = 0;
                state.LockedAt = null;
                state.LockReason = null;
                state.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                return new HeartbeatResult(true, "valid", body.Message);
            }

            if (body.Status is "revoked" or "expired")
            {
                state.Status = body.Status;
                state.LastHeartbeatStatus = body.Status;
                state.LockedAt = DateTimeOffset.UtcNow;
                state.LockReason = body.Status;
                state.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                return new HeartbeatResult(false, body.Status, body.Message);
            }

            return await FailHeartbeatAsync(state, "failed", body.Message, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "License heartbeat failed for license ending {Tail}", Tail(state.LicenseKey));
            return await FailHeartbeatAsync(state, "timeout", ex.Message, ct);
        }
    }

    public async Task<bool> IsModuleEnabledAsync(string moduleName, CancellationToken ct = default)
    {
        var state = await CurrentAsync(ct);
        if (state is null || IsLocked(state))
        {
            return false;
        }

        var modules = JsonNode.Parse(state.LicenseJson)?["licensed_modules"]?.AsArray()
            .Select(v => v?.GetValue<string>())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return modules?.Contains(moduleName) == true;
    }

    public async Task<LicenseRuntimeStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var state = await CurrentAsync(ct);
        if (state is null)
        {
            return new LicenseRuntimeStatus("pending_activation", true, "License is not activated.", null, null, 0, null);
        }

        var locked = IsLocked(state);
        return new LicenseRuntimeStatus(
            state.Status,
            locked,
            locked ? state.LockReason ?? "heartbeat_failed" : null,
            locked ? state.LockedAt ?? state.OverrideUntil : null,
            state.LastHeartbeatAt,
            state.FailedHeartbeatCount,
            state.OverrideUntil);
    }

    public async Task ManualOverrideAsync(Guid actorId, string reason, DateTimeOffset validUntil, CancellationToken ct = default)
    {
        var state = await CurrentAsync(ct) ?? throw new InvalidOperationException("License is not activated.");
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Override reason is required.");
        }

        if (validUntil <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Override expiry must be in the future.");
        }

        state.OverrideBy = actorId;
        state.OverrideReason = reason;
        state.OverrideUntil = validUntil;
        state.LockedAt = null;
        state.LockReason = null;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(new AuditEntry(actorId, null, "license.override", "license", "LicenseState", state.Id.ToString(), null, reason, null, null, null), ct);
    }

    private async Task<HeartbeatResult> FailHeartbeatAsync(LicenseState? state, string status, string message, CancellationToken ct)
    {
        if (state is null)
        {
            return new HeartbeatResult(false, status, message);
        }

        state.LastHeartbeatStatus = status;
        state.FailedHeartbeatCount++;
        if (state.FailedHeartbeatCount >= 2)
        {
            state.LockedAt = DateTimeOffset.UtcNow;
            state.LockReason = "heartbeat_failed";
        }

        state.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return new HeartbeatResult(false, status, message);
    }

    private async Task<LicenseState?> CurrentAsync(CancellationToken ct)
    {
        var states = await db.LicenseStates.ToListAsync(ct);
        return states.OrderByDescending(l => l.CreatedAt).FirstOrDefault();
    }

    private static bool IsLocked(LicenseState state)
        => (state.LockedAt is not null || state.FailedHeartbeatCount >= 2)
            && (state.OverrideUntil is null || state.OverrideUntil <= DateTimeOffset.UtcNow);

    private static LicenseValidationResult Fail(string message, LicenseValidationFailureReason reason)
        => new(false, message, null, reason);

    private static string Required(JsonObject payload, string key)
        => payload[key]?.ToString() ?? throw new InvalidOperationException($"License field '{key}' is required.");

    private static string Sign(JsonObject payload, string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(payload.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
        var key = Encoding.UTF8.GetBytes(secret);
        return Convert.ToHexString(HMACSHA256.HashData(key, bytes)).ToLowerInvariant();
    }

    private static bool FixedEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static string RedactSignature(string licenseKey)
    {
        var payload = JsonNode.Parse(licenseKey)?.AsObject() ?? [];
        payload["signature"] = "***";
        return payload.ToJsonString();
    }

    private static string Tail(string value) => value.Length <= 8 ? value : value[^8..];

    private sealed record HeartbeatResponse(string Status, string Message);
}
