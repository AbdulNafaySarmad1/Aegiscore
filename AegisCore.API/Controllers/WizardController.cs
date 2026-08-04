using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using System.Net.Mail;
using System.Text.RegularExpressions;
using AegisCore.API.Middleware;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/wizard")]
public sealed class WizardController(
    IConfiguration configuration,
    WizardSessionService sessions,
    WizardActivationService activation,
    WizardGatewayService gateways,
    WizardStateService state,
    OnboardingService onboarding,
    IInstallationStateService installation,
    ISecretService secrets) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxStepPayloadBytes = 128 * 1024;

    [HttpGet("session")]
    public async Task<IActionResult> GetSession(CancellationToken ct)
    {
        var session = await sessions.GetOrCreateAsync(TenantId(), HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(ToResponse(session));
    }

    [HttpPost("validate-license-key")]
    public async Task<IActionResult> ValidateLicense([FromBody] ValidateLicenseRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.LicenseKey)) return BadRequest(new { valid = false, error = "License key is required." });
        var result = await onboarding.ValidateLicenseAsync(state, request.LicenseKey, ct);
        return Ok(new { valid = result.IsValid, universityName = result.License?.IssuedTo, validUntil = result.License?.ExpiresAt, error = result.Error?.UserMessage });
    }

    [HttpPost("mode")]
    public async Task<IActionResult> ChangeMode([FromBody] ChangeWizardModeRequest request, CancellationToken ct)
    {
        var mode = request.Mode.Trim().ToLowerInvariant();
        if (mode is not ("demo" or "paid" or "live"))
            return UnprocessableEntity(new { success = false, error = "Mode must be demo or live." });

        var tenantId = TenantId();
        var session = await sessions.GetOrCreateAsync(tenantId, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (session.Id != request.SessionId)
            return UnprocessableEntity(new { success = false, error = "Wizard session was not found." });

        var normalizedMode = mode == "live" ? "paid" : mode;
        if (normalizedMode == "paid")
        {
            if (!HasProductionSecretProvider())
                return UnprocessableEntity(new { success = false, error = "Live mode requires Infisical or a supported cloud secret vault." });
            if (string.IsNullOrWhiteSpace(request.LicenseKey))
                return UnprocessableEntity(new { success = false, error = "A valid license key is required for live mode." });
            var license = await onboarding.ValidateLicenseAsync(state, request.LicenseKey, ct);
            if (!license.IsValid)
                return UnprocessableEntity(new { success = false, error = license.Error?.UserMessage ?? "License validation failed." });
        }

        if (normalizedMode == "demo")
            await gateways.DisableLiveConfigurationAsync(tenantId, ct);

        var changed = await sessions.ChangeModeAsync(tenantId, request.SessionId, normalizedMode, request.LicenseKey, ct);
        return Ok(new
        {
            success = true,
            mode = changed.LicenseMode,
            currentStep = Math.Min(changed.CurrentStep, 7),
            data = PublicStepData(changed.StepData)
        });
    }

    [HttpPost("step/{step:int}/save")]
    public async Task<IActionResult> SaveStep(int step, [FromBody] SaveStepRequest request, CancellationToken ct)
    {
        if (step is < 0 or > 7) return BadRequest(new { success = false, error = "Invalid wizard step." });
        if (Encoding.UTF8.GetByteCount(request.Data.GetRawText()) > MaxStepPayloadBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { success = false, error = "Wizard step payload exceeds the 128 KiB limit." });
        if (!ValidateStep(step, request.Data, out var error)) return UnprocessableEntity(new { success = false, error });
        if (step == 0 && request.Data.TryGetProperty("mode", out var mode) && mode.GetString() == "paid")
        {
            if (!HasProductionSecretProvider())
                return UnprocessableEntity(new { success = false, error = "Live mode requires Infisical or a supported cloud secret vault." });
            var licenseKey = request.Data.GetProperty("licenseKey").GetString() ?? string.Empty;
            var license = await onboarding.ValidateLicenseAsync(state, licenseKey, ct);
            if (!license.IsValid) return UnprocessableEntity(new { success = false, error = license.Error?.UserMessage ?? "License validation failed." });
        }
        var data = step == 3 ? SanitizeGatewayStep(request.Data) : step == 4 ? await SanitizeFinanceStep(request.Data, ct) : request.Data;
        try
        {
            var session = await sessions.SaveAsync(TenantId(), request.SessionId, step, data, ct);
            return Ok(new { success = true, nextStep = Math.Min(session.CurrentStep, 7), sessionId = session.Id });
        }
        catch (KeyNotFoundException)
        {
            return UnprocessableEntity(new { success = false, error = "Wizard session was not found." });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("gateways/{slug}/configure")]
    public async Task<IActionResult> ConfigureGateway(string slug, [FromBody] JsonElement request, CancellationToken ct)
    {
        var session = await sessions.GetOrCreateAsync(TenantId(), HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!string.Equals(session.LicenseMode, "paid", StringComparison.OrdinalIgnoreCase))
            return UnprocessableEntity(new { success = false, error = "Payment gateways are disabled in demo mode." });
        try
        {
            var result = await gateways.ConfigureAsync(TenantId(), slug, request, ct);
            await sessions.MarkGatewayConfiguredAsync(TenantId(), session.Id, result.GatewaySlug, ct);
            return Ok(result);
        }
        catch (ArgumentException ex) { return UnprocessableEntity(new { success = false, error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { success = false, error = ex.Message }); }
    }

    [HttpPost("gateways/{slug}/health-check")]
    public async Task<IActionResult> GatewayHealthCheck(string slug, CancellationToken ct)
    {
        try { return Ok(await gateways.HealthCheckAsync(TenantId(), slug, ct)); }
        catch (ArgumentException ex) { return UnprocessableEntity(new { success = false, error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { success = false, error = ex.Message }); }
    }

    [HttpPost("upload-branding/{type}"), RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadBranding(string type, CancellationToken ct)
    {
        if (type is not ("logo" or "background"))
            return BadRequest(new { error = "Type must be 'logo' or 'background'." });

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        ms.Position = 0;

        if (ms.Length < 1024 || ms.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = "File must be between 1KB and 10MB." });

        // Validate it's an actual image
        var header = new byte[4];
        ms.Read(header, 0, 4);
        ms.Position = 0;
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var isJpeg = header[0] == 0xFF && header[1] == 0xD8;
        if (!isPng && !isJpeg)
            return BadRequest(new { error = "Only PNG and JPEG images are accepted." });

        var contentType = isPng ? "image/png" : "image/jpeg";
        var ext = isPng ? "png" : "jpg";

        var storage = HttpContext.RequestServices.GetRequiredService<IStorageService>();
        var bucket = configuration["STORAGE_BUCKET"] ?? configuration["R2:BucketName"] ?? "aegiscore";
        await storage.CreateBucketIfNotExistsAsync(bucket, ct);
        var key = $"branding/{type}.{ext}";
        var url = await storage.UploadAsync(bucket, key, ms, contentType, StorageVisibility.Public, ct);

        // Store as config so it survives restarts
        var configKey = type == "logo" ? "Branding:ClientLogoUrl" : "Branding:LoginBackgroundUrl";
        await secrets.SetSecretAsync(configKey, url, ct);

        return Ok(new { url, type, message = $"{type} uploaded successfully." });
    }

    [HttpGet("branding/{type}")]
    public async Task<IActionResult> GetBranding(string type, CancellationToken ct)
    {
        if (type is not ("logo" or "background"))
            return BadRequest(new { error = "Type must be 'logo' or 'background'." });

        var configKey = type == "logo" ? "Branding:ClientLogoUrl" : "Branding:LoginBackgroundUrl";
        var url = configuration[configKey] ?? configuration[$"AEGISCORE_CLIENT_{type.ToUpperInvariant()}_URL"];
        if (string.IsNullOrWhiteSpace(url))
        {
            try
            {
                var bucket = configuration["STORAGE_BUCKET"] ?? "aegiscore";
                var storage = HttpContext.RequestServices.GetRequiredService<IStorageService>();
                var ext = type == "logo" ? "png" : "jpg";
                if (await storage.ExistsAsync(bucket, $"branding/{type}.{ext}", ct))
                    url = (await storage.GetPresignedUrlAsync(bucket, $"branding/{type}.{ext}", TimeSpan.FromHours(24), ct)) ?? "";
            }
            catch { }
        }

        return Ok(new { url = url ?? "" });
    }

    [HttpPost("activate-demo")]
    public async Task<IActionResult> ActivateDemo([FromQuery] int expiryDays, CancellationToken ct)
    {
        var isDemo = configuration.GetValue<bool>("Runtime:DemoMode");
        if (!isDemo) return BadRequest(new { error = "Demo mode is not enabled. Set Runtime:DemoMode=true in .env to use this feature." });

        var existingExpiry = configuration["Runtime:DemoExpiryUtc"];
        if (!string.IsNullOrWhiteSpace(existingExpiry))
            return BadRequest(new { error = "Demo expiry has already been set and cannot be changed. Current expiry: " + existingExpiry });

        if (expiryDays < 1 || expiryDays > 365)
            return BadRequest(new { error = "Expiry must be between 1 and 365 days." });

        var expiry = DateTime.UtcNow.AddDays(expiryDays).ToString("O");
        var hmacKey = configuration["AEGISCORE_ENCRYPTION_KEY"] ?? await secrets.GetSecretAsync("AEGISCORE_ENCRYPTION_KEY", ct);
        var signature = DemoExpiryMiddleware.SignExpiry(expiry, hmacKey);

        await secrets.SetSecretAsync("Runtime__DemoExpiryUtc", expiry, ct);
        await secrets.SetSecretAsync("Runtime__DemoExpirySignature", signature, ct);

        return Ok(new
        {
            message = $"Demo activated. Expires in {expiryDays} days on {expiry}.",
            expiry,
            immutable = true,
            note = "Once set, the demo expiry CANNOT be changed. The signature prevents tampering."
        });
    }

    [AllowAnonymous]
    [HttpGet("demo-status")]
    public IActionResult DemoStatus()
    {
        var isDemo = configuration.GetValue<bool>("Runtime:DemoMode");
        if (!isDemo) return Ok(new { isDemo = false });

        var expiry = configuration["Runtime:DemoExpiryUtc"];
        TimeSpan? remaining = null;
        if (DateTime.TryParse(expiry, out var exp))
            remaining = exp - DateTime.UtcNow;

        return Ok(new
        {
            isDemo = true,
            expiryUtc = expiry,
            remainingDays = remaining?.TotalDays ?? -1,
            remainingHours = remaining?.TotalHours ?? -1,
            isExpired = remaining.HasValue && remaining.Value.TotalSeconds <= 0
        });
    }

    [HttpPost("generate-secrets")]
    public async Task<IActionResult> GenerateSecrets(CancellationToken ct)
    {
        var provider = (configuration["AEGISCORE_SECRET_PROVIDER"] ?? "FILE").Trim().ToUpperInvariant();
        if (provider is "INFISICAL" or "AZURE" or "AWS" or "GCP")
            return Ok(new { success = true, message = "Using external secrets provider — no local generation needed.", provider });

        var generated = new Dictionary<string, string>();
        var requiredSecrets = new[] {
            "AEGISCORE_JWT_SECRET", "AEGISCORE_ENCRYPTION_KEY", "AEGISCORE_PQC_KEY_BASE64",
            "LICENSE_HMAC_SECRET", "GOVERNANCE_FEEDBACK_KEY_BASE64"
        };

        foreach (var key in requiredSecrets)
        {
            var existing = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(existing)) { generated[key] = "already-set"; continue; }

            var value = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
            await secrets.SetSecretAsync(key, value, ct);
            generated[key] = "generated";
        }

        return Ok(new { success = true, message = $"{generated.Count(kv => kv.Value == "generated")} secrets generated, {generated.Count(kv => kv.Value == "already-set")} already present.", generated });
    }

    [HttpGet("secrets-status")]
    public async Task<IActionResult> SecretsStatus(CancellationToken ct)
    {
        var requiredSecrets = new[] { "AEGISCORE_JWT_SECRET", "AEGISCORE_ENCRYPTION_KEY", "AEGISCORE_PQC_KEY_BASE64", "LICENSE_HMAC_SECRET" };
        var status = new Dictionary<string, bool>();
        foreach (var key in requiredSecrets)
            status[key] = await secrets.SecretExistsAsync(key, ct) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key));
        var allPresent = status.Values.All(v => v);
        var provider = (configuration["AEGISCORE_SECRET_PROVIDER"] ?? "FILE").Trim().ToUpperInvariant();
        return Ok(new { allPresent, provider, needsGeneration = provider is not ("INFISICAL" or "AZURE" or "AWS" or "GCP") && !allPresent, secrets = status });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteWizardRequest request, CancellationToken ct)
    {
        var tenantId = TenantId();
        try
        {
            await activation.ActivateAsync(tenantId, request.SessionId, request.ConfirmedBy, ct);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return UnprocessableEntity(new { success = false, error = ex.Message });
        }
        await installation.MarkAsInstalledAsync(ct);
        return Ok(new { success = true, tenantId, dashboardUrl = "/", adminEmail = request.ConfirmedBy });
    }

    private Guid TenantId() => Guid.TryParse(configuration["TENANT_ID"], out var configured) ? configured : state.TenantId;

    private bool HasProductionSecretProvider()
        => (configuration["AEGISCORE_SECRET_PROVIDER"] ?? "INFISICAL").Trim().ToUpperInvariant() is "INFISICAL" or "AZURE" or "AWS" or "GCP";

    private static object ToResponse(AegisCore.Domain.Entities.WizardSession session) => new
    {
        sessionId = session.Id, currentStep = Math.Min(session.CurrentStep, 7), licenseMode = session.LicenseMode,
        completedAt = session.CompletedAt, data = PublicStepData(session.StepData)
    };

    private static JsonNode PublicStepData(string serialized)
    {
        var root = JsonNode.Parse(serialized)?.AsObject() ?? new JsonObject();
        if (root["step0"] is JsonObject licenseStep)
            licenseStep.Remove("licenseKey");
        foreach (var step in root.Select(property => property.Value).OfType<JsonObject>())
        {
            foreach (var property in step.ToList())
            {
                if (Encoding.UTF8.GetByteCount(property.Value?.ToJsonString() ?? "null") > 64 * 1024)
                    step.Remove(property.Key);
            }
        }
        return root;
    }

    private static JsonElement SanitizeGatewayStep(JsonElement data)
    {
        var output = new JsonObject();
        foreach (var name in new[] { "enabledGateways", "configuredGateways", "mode" })
            if (data.TryGetProperty(name, out var value)) output[name] = JsonNode.Parse(value.GetRawText());
        return JsonSerializer.SerializeToElement(output, JsonOptions);
    }

    private async Task<JsonElement> SanitizeFinanceStep(JsonElement data, CancellationToken ct)
    {
        var output = new JsonObject();
        foreach (var property in data.EnumerateObject())
        {
            if (string.Equals(property.Name, "apiKey", StringComparison.OrdinalIgnoreCase)) continue;
            output[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }
        var rateSource = data.TryGetProperty("rateSource", out var source) && source.ValueKind == JsonValueKind.String ? source.GetString()?.Trim().ToUpperInvariant() : string.Empty;
        if (rateSource == "OPEN_EXCHANGE_RATES" && data.TryGetProperty("apiKey", out var apiKey) && apiKey.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(apiKey.GetString()))
        {
            var secretName = $"/tenants/{TenantId():D}/finance/exchange-rates/OER_API_KEY";
            await secrets.SetSecretAsync(secretName, apiKey.GetString()!, ct);
            output["rateApiKeySecretName"] = secretName;
        }
        return JsonSerializer.SerializeToElement(output, JsonOptions);
    }

    private static bool ValidateStep(int step, JsonElement data, out string error)
    {
        error = "";
        bool Has(params string[] names) => names.All(name => data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()));
        var mode = data.TryGetProperty("mode", out var modeValue) ? modeValue.GetString() : null;
        switch (step)
        {
            case 0 when mode is not ("demo" or "paid"):
                error = "Choose Demo or Licensed mode."; return false;
            case 0 when mode == "paid" && (!data.TryGetProperty("licenseKey", out var key) || string.IsNullOrWhiteSpace(key.GetString())):
                error = "A validated license key is required."; return false;
            case 1 when !Has("name", "shortName", "type", "country", "city", "language", "timezone", "street", "province", "contactName", "contactEmail", "contactPhone"):
                error = "Complete the required university and primary contact fields."; return false;
            case 1 when !IsEmail(data, "contactEmail") || !IsE164Phone(data, "contactPhone"):
                error = "Enter a valid contact email and E.164 phone number."; return false;
            case 2 when !Has("academicYear", "calendarType", "semesterStart", "semesterEnd"):
                error = "Complete the academic calendar."; return false;
            case 2 when !HasNamedRows(data, "faculties") || !HasNamedRows(data, "departments") || !HasNamedRows(data, "programmes"):
                error = "Add at least one faculty, department, and programme."; return false;
            case 3 when mode != "demo" && !HasConfiguredGateway(data):
                error = "Enable at least one configured payment gateway."; return false;
            case 4 when !Has("standard", "fiscalYear", "baseCurrency", "rateSource"):
                error = "Complete the finance configuration."; return false;
            case 4 when !new[] { "IFRS", "US_GAAP", "FRS_102", "IPSAS", "AAOIFI" }.Contains(data.GetProperty("standard").GetString()?.Trim().ToUpperInvariant().Replace(' ', '_')):
                error = "Select a supported accounting standard."; return false;
            case 4 when !IsCurrency(data, "baseCurrency"):
                error = "Enter a valid ISO base currency."; return false;
            case 4 when !new[] { "ECB", "SBP", "OPEN_EXCHANGE_RATES", "MANUAL" }.Contains(data.GetProperty("rateSource").GetString()?.Trim().ToUpperInvariant().Replace(' ', '_')):
                error = "Select a supported exchange-rate source."; return false;
            case 4 when data.GetProperty("rateSource").GetString()?.Trim().ToUpperInvariant().Replace(' ', '_') == "OPEN_EXCHANGE_RATES" && (!data.TryGetProperty("apiKey", out var apiKey) || apiKey.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(apiKey.GetString())):
                error = "An Open Exchange Rates API key is required."; return false;
            case 5 when !HasFeeConfiguration(data):
                error = "Define at least two fee heads and one programme assignment."; return false;
            case 6 when !HasAdministratorAccounts(data):
                error = "Create valid Super Admin and Finance Controller accounts."; return false;
            case 7 when !data.TryGetProperty("confirmed", out var confirmed) || confirmed.ValueKind != JsonValueKind.True:
                error = "Both legal confirmations are required."; return false;
        }
        return true;
    }

    private static bool HasArray(JsonElement data, string name) => data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0;

    private static bool HasNamedRows(JsonElement data, string name)
        => data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array && value.EnumerateArray().Any(row =>
            row.ValueKind == JsonValueKind.Object && row.TryGetProperty("name", out var rowName) && rowName.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(rowName.GetString()));

    private static bool HasConfiguredGateway(JsonElement data)
    {
        if (!data.TryGetProperty("enabledGateways", out var enabled) || enabled.ValueKind != JsonValueKind.Array ||
            !data.TryGetProperty("configuredGateways", out var configured) || configured.ValueKind != JsonValueKind.Array) return false;
        var configuredSet = configured.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()).Where(x => x is not null).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return enabled.EnumerateArray().Any(x => x.ValueKind == JsonValueKind.String && x.GetString() is { } gateway && configuredSet.Contains(gateway));
    }

    private static bool HasFeeConfiguration(JsonElement data)
        => data.TryGetProperty("feeHeads", out var heads) && heads.ValueKind == JsonValueKind.Array && heads.GetArrayLength() >= 2 &&
           data.TryGetProperty("assignments", out var assignments) && assignments.ValueKind == JsonValueKind.Array && assignments.GetArrayLength() > 0;

    private static bool HasAdministratorAccounts(JsonElement data)
    {
        if (!data.TryGetProperty("users", out var users) || users.ValueKind != JsonValueKind.Array) return false;
        var valid = users.EnumerateArray().Where(user => user.ValueKind == JsonValueKind.Object).Where(user =>
            user.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(name.GetString()) &&
            user.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String && IsEmail(email.GetString()) &&
            user.TryGetProperty("password", out var password) && password.ValueKind == JsonValueKind.String && password.GetString()?.Length >= 12 &&
            user.TryGetProperty("role", out var role) && role.ValueKind == JsonValueKind.String);
        var roles = valid.Select(user => user.GetProperty("role").GetString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roles.Contains("Super Admin") && roles.Contains("Finance Controller");
    }

    private static bool IsEmail(JsonElement data, string name) => data.TryGetProperty(name, out var value) && IsEmail(value.GetString());

    private static bool IsCurrency(JsonElement data, string name)
        => data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: 3 } currency && currency.All(c => c is >= 'A' and <= 'Z');

    private static bool IsEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 320) return false;
        try { return new MailAddress(value).Address.Equals(value, StringComparison.OrdinalIgnoreCase); }
        catch (FormatException) { return false; }
    }

    private static bool IsE164Phone(JsonElement data, string name)
        => data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: <= 16 } phone && Regex.IsMatch(phone, "^\\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant);
}

public sealed record ValidateLicenseRequest(string LicenseKey);
public sealed record ChangeWizardModeRequest(Guid SessionId, string Mode, string? LicenseKey);
public sealed record SaveStepRequest(Guid SessionId, JsonElement Data);
public sealed record CompleteWizardRequest(Guid SessionId, string ConfirmedBy, DateTimeOffset Timestamp);
