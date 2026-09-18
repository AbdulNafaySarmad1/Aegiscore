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

        if (OfflineLicense.IsOfflineKey(licenseKey))
            return OfflineLicense.Validate(licenseKey.Trim(), OfflineLicense.IssuerPublicKeyPem(configuration), DateOnly.FromDateTime(DateTime.UtcNow));

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
        state.TenantId = InstallationTenantId();
        state.UniversityId = validation.License.UniversityId;
        state.UniversityName = validation.License.UniversityName;
        state.Status = "active";
        state.LicenseJson = OfflineLicense.IsOfflineKey(licenseKey) ? OfflineLicense.PayloadJson(licenseKey.Trim()) : RedactSignature(licenseKey);
        if (OfflineLicense.IsOfflineKey(licenseKey)) state.LicenseKey = licenseKey.Trim();
        state.ActivatedAt ??= DateTimeOffset.UtcNow;
        // A newly activated key starts clean: the failures that locked the previous key do not carry over.
        state.FailedHeartbeatCount = 0;
        state.LastHeartbeatStatus = null;
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

        // An offline licence is the authority: it is re-verified against the vendor's public key on
        // every heartbeat and nothing is transmitted. There is no demo flag that skips this.
        if (OfflineModeEnabled() || OfflineLicense.IsOfflineKey(state.LicenseKey))
        {
            var validation = await ValidateAsync(state.LicenseKey, ct);
            if (!validation.IsValid)
            {
                var status = validation.FailureReason == LicenseValidationFailureReason.Expired ? "expired" : "invalid";
                state.Status = status;
                state.LastHeartbeatStatus = "offline_validation_failed";
                state.LockedAt = DateTimeOffset.UtcNow;
                state.LockReason = status;
                state.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                return new HeartbeatResult(false, status, validation.ValidationMessage);
            }

            state.Status = "active";
            state.LastHeartbeatAt = DateTimeOffset.UtcNow;
            state.LastHeartbeatStatus = "offline";
            state.FailedHeartbeatCount = 0;
            state.LockedAt = null;
            state.LockReason = null;
            state.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return new HeartbeatResult(
                true,
                "offline",
                "The offline licence was verified locally against the vendor public key; nothing was transmitted.");
        }

        var endpoint = configuration["License:HeartbeatUrl"] ?? configuration["License:ServerUrl"] ?? configuration["LICENSE_SERVER_URL"];
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
                current_user_count = await db.Users.IgnoreQueryFilters().CountAsync(u => u.TenantId == state.TenantId, ct),
                suite = "complete",
                timestamp = DateTimeOffset.UtcNow
            }, timeout.Token);

            var body = await response.Content.ReadFromJsonAsync<HeartbeatResponse>(cancellationToken: timeout.Token)
                ?? new HeartbeatResponse("invalid", "Empty heartbeat response.");

            if (response.IsSuccessStatusCode && body.Status == "valid")
            {
                if (!TryTier(body.Tier ?? "Basic", out var tier))
                    return await LockAsync(state, "invalid", "License authority returned an unknown tier.", ct);
                state.Status = "active";
                var metadata = JsonNode.Parse(state.LicenseJson)?.AsObject() ?? new JsonObject();
                metadata["tier"] = tier.ToString();
                state.LicenseJson = metadata.ToJsonString();
                state.LastHeartbeatAt = DateTimeOffset.UtcNow;
                state.LastHeartbeatStatus = "success";
                state.FailedHeartbeatCount = 0;
                state.LockedAt = null;
                state.LockReason = null;
                state.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                return new HeartbeatResult(true, "valid", body.Message);
            }

            if (body.Status is "revoked" or "expired" or "invalid")
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
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "License heartbeat failed for license ending {Tail}", Tail(state.LicenseKey));
            return await FailHeartbeatAsync(state, "timeout", ex.Message, ct);
        }
    }

    public async Task<LicenseRuntimeStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var state = await CurrentAsync(ct);
        if (state is null)
        {
            return new LicenseRuntimeStatus("pending_activation", true, "pending_activation", null, null, 0, null, LicenseTier.Basic,
                UniversityId: InstallationTenantId(), Severity: "locked",
                Message: "No licence has been activated on this installation, so it is read-only.",
                NextStep: "Paste the licence key the vendor issued for this university id below.");
        }

        var locked = IsLocked(state);
        var (severity, message, nextStep, graceUntil) = Explain(state, locked);
        return new LicenseRuntimeStatus(
            state.Status,
            locked,
            locked ? state.LockReason ?? "heartbeat_failed" : null,
            locked ? state.LockedAt ?? state.OverrideUntil : null,
            state.LastHeartbeatAt,
            state.FailedHeartbeatCount,
            state.OverrideUntil,
            OfflineLicense.IsOfflineKey(state.LicenseKey) ? (TryTier(OfflineLicense.Tier(state.LicenseKey), out var offlineTier) ? offlineTier : LicenseTier.Basic)
                : ReadTier(OfflineModeEnabled() ? state.LicenseKey : state.LicenseJson),
            UniversityId: state.UniversityId, UniversityName: state.UniversityName, Severity: severity, Message: message, NextStep: nextStep,
            GraceUntil: graceUntil, IsOffline: OfflineLicense.IsOfflineKey(state.LicenseKey), ExpiryDate: ExpiryOf(state));
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
        // Graceful degradation: the first failure warns; the lock engages once failures have persisted
        // for the grace period (License:GracePeriodDays, default 7) since the last successful heartbeat,
        // and never before a second consecutive failure. Every read still works after the lock.
        if (state.FailedHeartbeatCount >= 2 && GraceUntil(state) <= DateTimeOffset.UtcNow)
        {
            state.LockedAt = DateTimeOffset.UtcNow;
            state.LockReason = "heartbeat_failed";
        }

        state.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return new HeartbeatResult(false, status, message);
    }

    private bool OfflineModeEnabled() =>
        configuration.GetValue<bool>("License:OfflineMode")
        || bool.TryParse(configuration["LICENSE_OFFLINE_MODE"], out var enabled) && enabled;

    private async Task<LicenseState?> CurrentAsync(CancellationToken ct)
    {
        var tenantId = InstallationTenantId();
        var states = await db.LicenseStates.IgnoreQueryFilters().Where(s => s.TenantId == tenantId).ToListAsync(ct);
        return states.OrderByDescending(l => l.CreatedAt).FirstOrDefault();
    }

    private Guid InstallationTenantId() => Guid.TryParse(configuration["TENANT_ID"], out var id) ? id : db.CurrentTenantId;

    private async Task<HeartbeatResult> LockAsync(LicenseState state, string status, string message, CancellationToken ct)
    {
        state.Status = status;
        state.LastHeartbeatStatus = status;
        state.LockedAt = DateTimeOffset.UtcNow;
        state.LockReason = status;
        await db.SaveChangesAsync(ct);
        return new(false, status, message);
    }

    private static bool TryTier(string value, out LicenseTier tier) =>
        Enum.TryParse(value, true, out tier) && Enum.IsDefined(tier);

    private static LicenseTier ReadTier(string json)
    {
        try { return TryTier(JsonNode.Parse(json)?["tier"]?.ToString() ?? "Basic", out var tier) ? tier : LicenseTier.Basic; }
        catch (JsonException) { return LicenseTier.Basic; }
    }

    private bool IsLocked(LicenseState state)
        => (state.Status is "expired" or "invalid" or "revoked" || state.LockedAt is not null
                || (state.FailedHeartbeatCount >= 2 && GraceUntil(state) <= DateTimeOffset.UtcNow))
            && (state.OverrideUntil is null || state.OverrideUntil <= DateTimeOffset.UtcNow);

    private int GracePeriodDays => int.TryParse(configuration["License:GracePeriodDays"], out var days) && days >= 0 ? days : 7;

    /// <summary>When failing heartbeats turn into a lock: the grace period after the last success (or after activation, if none).</summary>
    private DateTimeOffset GraceUntil(LicenseState state)
        => (state.LastHeartbeatAt ?? state.ActivatedAt ?? state.CreatedAt).AddDays(GracePeriodDays);

    /// <summary>The sentence and the next step for the person looking at the licence page.</summary>
    private (string Severity, string Message, string NextStep, DateTimeOffset? GraceUntil) Explain(LicenseState state, bool locked)
    {
        var offline = OfflineLicense.IsOfflineKey(state.LicenseKey);
        var expiry = ExpiryOf(state);
        if (state.OverrideUntil > DateTimeOffset.UtcNow)
            return ("warning", $"A manual override keeps this installation writable until {state.OverrideUntil:d MMMM yyyy HH:mm} UTC ({state.OverrideReason}).",
                "Before the override ends, enter a valid licence key on this page or restore the connection to the licence authority.", null);
        if (locked)
        {
            var cause = state.LockReason switch
            {
                "heartbeat_failed" => "the licence authority could not be reached for " + state.FailedHeartbeatCount + " consecutive daily heartbeats",
                "expired" => "the licence expired" + (expiry is { } e ? $" on {e:d MMMM yyyy}" : ""),
                "revoked" => "the vendor revoked the licence",
                "invalid" => "the licence key failed verification",
                _ => "the licence is " + (state.LockReason ?? state.Status)
            };
            return ("locked", $"This installation is read-only because {cause}. Everyone can still sign in, read and export; nothing can be changed.",
                offline || state.LockReason is "expired" or "revoked" or "invalid"
                    ? "Ask the vendor for a new offline licence key issued for this university id and paste it below. Access is restored the moment it is accepted."
                    : "If this network can reach the vendor, restore the connection (LICENSE_SERVER_URL) and press \"Check now\". If it cannot — most campus networks cannot — ask the vendor for an offline licence key issued for this university id and paste it below. Either restores write access immediately.", null);
        }
        if (state.FailedHeartbeatCount > 0 && !offline)
        {
            var until = GraceUntil(state);
            return ("warning", $"The last {state.FailedHeartbeatCount} heartbeat(s) to the licence authority failed (\"{state.LastHeartbeatStatus}\"). The installation stays fully usable until {until:d MMMM yyyy HH:mm} UTC; after that it becomes read-only unless a heartbeat succeeds.",
                "Check that this server can reach the licence authority, or enter an offline licence key from the vendor below to stop depending on the connection.", until);
        }
        if (expiry is { } d && d <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30))
            return ("warning", $"The licence expires on {d:d MMMM yyyy}.", "Ask the vendor for a renewed key before that date and paste it below.", null);
        return ("ok", offline
                ? $"Offline licence for {state.UniversityName} is active" + (expiry is { } x ? $" until {x:d MMMM yyyy}" : "") + "; it is verified locally and no authority is contacted."
                : $"Licence for {state.UniversityName} is active; the last heartbeat succeeded" + (state.LastHeartbeatAt is { } h ? $" at {h:d MMMM yyyy HH:mm} UTC" : "") + ".",
            "Nothing to do.", null);
    }

    private static DateOnly? ExpiryOf(LicenseState state)
    {
        try
        {
            var json = OfflineLicense.IsOfflineKey(state.LicenseKey) ? OfflineLicense.PayloadJson(state.LicenseKey) : state.LicenseJson;
            var value = JsonNode.Parse(json)?["expiry_date"]?.ToString();
            return DateOnly.TryParse(value, out var d) ? d : null;
        }
        catch { return null; }
    }

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

    private sealed record HeartbeatResponse(string Status, string Message, string? Tier = null);
}
