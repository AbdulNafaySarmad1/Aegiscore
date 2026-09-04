using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using System.Net.Mail;
using System.Text.RegularExpressions;
using AegisCore.API.Middleware;
using AegisCore.Application.DataImport;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain.Entities;
using AegisCore.Application.Onboarding;
using Microsoft.EntityFrameworkCore;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Security;
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
        var session = await sessions.GetOrCreateAsync(SetupToken(), HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return Ok(ToResponse(session));
    }

    [HttpGet("resume")]
    public async Task<IActionResult> Resume(CancellationToken ct)
    {
        try
        {
            return Ok(ToResponse(await state.ResumeWizardAsync(SetupToken(), ct)));
        }
        catch (WizardSessionNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (WizardSessionCompletedException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("validate-license-key")]
    public async Task<IActionResult> ValidateLicense([FromBody] ValidateLicenseRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.LicenseKey)) return BadRequest(new { valid = false, error = "License key is required." });
        var token = SetupToken();
        var wizardState = await state.GetOrCreateStateAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        var result = await onboarding.ValidateLicenseAsync(wizardState, request.LicenseKey, ct);
        await state.SaveState(token, wizardState, ct);
        return Ok(new { valid = result.IsValid, universityName = result.License?.IssuedTo, validUntil = result.License?.ExpiresAt, error = result.Error?.UserMessage });
    }

    [HttpPost("mode")]
    public async Task<IActionResult> ChangeMode([FromBody] ChangeWizardModeRequest request, CancellationToken ct)
    {
        var mode = request.Mode.Trim().ToLowerInvariant();
        if (mode is not ("demo" or "paid" or "live"))
            return UnprocessableEntity(new { success = false, error = "Mode must be demo or live." });

        var token = SetupToken();
        var session = await sessions.GetOrCreateAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (session.SessionId != request.SessionId)
            return UnprocessableEntity(new { success = false, error = "Wizard session was not found." });

        var normalizedMode = mode == "live" ? "paid" : mode;
        if (normalizedMode == "paid")
        {
            if (!HasProductionSecretProvider())
                return UnprocessableEntity(new { success = false, error = "Live mode requires Infisical or a supported cloud secret vault." });
            if (string.IsNullOrWhiteSpace(request.LicenseKey))
                return UnprocessableEntity(new { success = false, error = "A valid license key is required for live mode." });
            var license = await onboarding.ValidateLicenseAsync(session, request.LicenseKey, ct);
            if (!license.IsValid)
                return UnprocessableEntity(new { success = false, error = license.Error?.UserMessage ?? "License validation failed." });
            await state.SaveState(token, session, ct);
        }

        if (normalizedMode == "demo")
        {
            if (session.TenantId != Guid.Empty)
                await gateways.DisableLiveConfigurationAsync(session.TenantId, ct);
        }

        var changed = await sessions.ChangeModeAsync(token, request.SessionId, normalizedMode, request.LicenseKey, ct);
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
        var token = SetupToken();
        if (step is < 0 or > 7) return BadRequest(new { success = false, error = "Invalid wizard step." });
        if (Encoding.UTF8.GetByteCount(request.Data.GetRawText()) > MaxStepPayloadBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { success = false, error = "Wizard step payload exceeds the 128 KiB limit." });
        if (!ValidateStep(step, request.Data, out var error)) return UnprocessableEntity(new { success = false, error });
        if (step == 0 && request.Data.TryGetProperty("mode", out var mode) && mode.GetString() == "paid")
        {
            if (!HasProductionSecretProvider())
                return UnprocessableEntity(new { success = false, error = "Live mode requires Infisical or a supported cloud secret vault." });
            var licenseKey = request.Data.GetProperty("licenseKey").GetString() ?? string.Empty;
            var wizardState = await state.GetOrCreateStateAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            var license = await onboarding.ValidateLicenseAsync(wizardState, licenseKey, ct);
            if (!license.IsValid) return UnprocessableEntity(new { success = false, error = license.Error?.UserMessage ?? "License validation failed." });
            await state.SaveState(token, wizardState, ct);
        }
        var data = step == 3 ? SanitizeGatewayStep(request.Data) : step == 4 ? await SanitizeFinanceStep(request.Data, ct) : request.Data;
        try
        {
            var session = await sessions.SaveAsync(token, request.SessionId, step, data, ct);
            return Ok(new { success = true, nextStep = Math.Min(session.CurrentStep, 7), sessionId = session.SessionId });
        }
        catch (KeyNotFoundException)
        {
            return UnprocessableEntity(new { success = false, error = "Wizard session was not found." });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { success = false, error = ex.Message });
        }
        catch (DbUpdateException)
        {
            return UnprocessableEntity(new { success = false, error = "Could not save administrator accounts. Ensure every email is unique and passwords are at least 12 characters." });
        }
    }

    [HttpPost("gateways/{slug}/configure")]
    public async Task<IActionResult> ConfigureGateway(string slug, [FromBody] JsonElement request, CancellationToken ct)
    {
        var token = SetupToken();
        var session = await sessions.GetOrCreateAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        try
        {
            if (session.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before configuring gateways.");
            var demoMode = !string.Equals(session.LicenseMode, "paid", StringComparison.OrdinalIgnoreCase);
            var result = await gateways.ConfigureAsync(session.TenantId, slug, request, ct, demoMode: demoMode);
            await sessions.MarkGatewayConfiguredAsync(token, session.SessionId, result.GatewaySlug, ct);
            return Ok(result);
        }
        catch (ArgumentException ex) { return UnprocessableEntity(new { success = false, error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { success = false, error = ex.Message }); }
    }

    [HttpPost("gateways/{slug}/health-check")]
    public async Task<IActionResult> GatewayHealthCheck(string slug, CancellationToken ct)
    {
        try
        {
            var wizardState = await state.ResumeWizardAsync(SetupToken(), ct);
            if (wizardState.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before checking gateways.");
            return Ok(await gateways.HealthCheckAsync(wizardState.TenantId, slug, ct));
        }
        catch (ArgumentException ex) { return UnprocessableEntity(new { success = false, error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { success = false, error = ex.Message }); }
    }

    [HttpPost("upload-branding/{type}"), RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadBranding(string type, CancellationToken ct)
    {
        if (type is not ("logo" or "background"))
            return BadRequest(new { error = "Type must be 'logo' or 'background'." });

        var contentDisposition = Request.Headers.ContentDisposition.ToString();
        if (UploadValidationService.HasDirectoryTraversal(contentDisposition))
            return BadRequest(new { error = "Invalid file upload." });

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        ms.Position = 0;

        var validator = new UploadValidationService();
        var validationResult = validator.ValidateMagicBytes(ms, "png", "jpeg");
        if (!validationResult.IsValid)
            return BadRequest(new { error = validationResult.ErrorMessage });

        var contentType = validationResult.DetectedContentType!;
        var ext = validationResult.DetectedExtension!;

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
    public async Task<IActionResult> DemoStatus()
    {
        var isDemo = configuration.GetValue<bool>("Runtime:DemoMode");
        if (!isDemo) return Ok(new { isDemo = false });

        var expiry = configuration["Runtime:DemoExpiryUtc"];
        if (string.IsNullOrWhiteSpace(expiry))
        {
            try { expiry = await secrets.GetSecretAsync("Runtime__DemoExpiryUtc"); }
            catch { /* secret not set yet — no demo expiry configured */ }
        }
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

    [HttpPost("import-demo-data")]
    public async Task<IActionResult> ImportDemoData([FromBody] DemoImportRequest request, [FromServices] IDataImportService dataImport, CancellationToken ct)
    {
        var isDemo = configuration.GetValue<bool>("Runtime:DemoMode");
        if (!isDemo) return BadRequest(new { error = "Demo mode is not enabled. Set Runtime:DemoMode=true to use this feature." });

        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest(new { error = "Connection string is required." });
        if (request.DbType is not ("PostgreSQL" or "MySQL" or "MSSQL" or "Oracle"))
            return BadRequest(new { error = "Unsupported database type. Use PostgreSQL, MySQL, MSSQL, or Oracle." });

        var wizardState = await state.ResumeWizardAsync(SetupToken(), ct);
        if (wizardState.TenantId == Guid.Empty) return UnprocessableEntity(new { error = "Complete wizard Step 1 before importing data." });
        var conn = new ExternalDbConnection
        {
            Name = $"Demo import — {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
            DbType = request.DbType,
            ConnectionString = request.ConnectionString,
            TenantId = wizardState.TenantId
        };

        try
        {
            conn = await dataImport.AddConnectionAsync(conn, ct);
            var connectionOk = await dataImport.TestConnectionAsync(conn.Id, ct);
            if (!connectionOk)
                return BadRequest(new { error = "Connection test failed. Verify your connection string and ensure the target database is reachable from this server." });

            var tables = await dataImport.DiscoverTablesAsync(conn.Id, null, ct);
            var autoMapped = AutoMapTables(tables);

            var importResults = new List<object>();
            foreach (var (table, entityType) in autoMapped)
            {
                try
                {
                    var columns = await dataImport.DiscoverColumnsAsync(conn.Id, table, null, ct);
                    var fieldMappings = AutoMapColumns(entityType, columns);
                    if (fieldMappings.Count < 2)
                    {
                        importResults.Add(new { table, entityType, status = "Skipped", reason = "Too few auto-mapped columns (minimum 2 required)." });
                        continue;
                    }

                    var query = request.DbType == "PostgreSQL"
                        ? $"SELECT * FROM \"{table.Replace("\"", "\"\"")}\" LIMIT 1000"
                        : $"SELECT * FROM [{table.Replace("]", "]]")}]";

                    var job = await dataImport.StartImportAsync(conn.Id, entityType, query, fieldMappings, null, ct);
                    importResults.Add(new { table, entityType, job.Id, job.Status, job.TotalRows, job.SuccessRows, job.ErrorRows, job.SkippedRows });
                }
                catch (Exception ex)
                {
                    importResults.Add(new { table, entityType, status = "Failed", reason = ex.Message });
                }
            }

            return Ok(new
            {
                success = true,
                connectionId = conn.Id,
                tablesDiscovered = tables.Count,
                tablesAutoMapped = autoMapped.Count,
                autoMappedTables = autoMapped.Select(m => new { m.Table, m.EntityType }).ToList(),
                results = importResults,
                note = request.DbType != "PostgreSQL" ? "Non-PostgreSQL sources use simulated discovery. Only PostgreSQL provides real column metadata and actual row import." : null
            });
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(new { success = false, error = $"Import pipeline failed: {ex.Message}" });
        }
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
        try
        {
            var completed = await activation.ActivateAsync(SetupToken(), request.SessionId, request.ConfirmedBy, ct);
            await installation.MarkAsInstalledAsync(ct);

            var isDemo = configuration.GetValue<bool>("Runtime:DemoMode");
            if (isDemo)
            {
                var existingExpiry = configuration["Runtime:DemoExpiryUtc"];
                if (string.IsNullOrWhiteSpace(existingExpiry))
                {
                    var expiryDays = 60;
                    var expiry = DateTime.UtcNow.AddDays(expiryDays).ToString("O");
                    var hmacKey = configuration["AEGISCORE_ENCRYPTION_KEY"] ?? await secrets.GetSecretAsync("AEGISCORE_ENCRYPTION_KEY", ct);
                    var signature = DemoExpiryMiddleware.SignExpiry(expiry, hmacKey);
                    await secrets.SetSecretAsync("Runtime__DemoExpiryUtc", expiry, ct);
                    await secrets.SetSecretAsync("Runtime__DemoExpirySignature", signature, ct);
                }
            }

            return Ok(new { success = true, tenantId = completed.TenantId, dashboardUrl = "/", adminEmail = request.ConfirmedBy });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return UnprocessableEntity(new { success = false, error = ex.Message });
        }
    }

    private string SetupToken()
        => Request.Headers["X-Setup-Token"].FirstOrDefault()
            ?? configuration["AEGISCORE_SETUP_TOKEN"]
            ?? throw new InvalidOperationException("AEGISCORE_SETUP_TOKEN is required.");

    private bool HasProductionSecretProvider()
        => (configuration["AEGISCORE_SECRET_PROVIDER"] ?? "INFISICAL").Trim().ToUpperInvariant() is "INFISICAL" or "AZURE" or "AWS" or "GCP";

    private static object ToResponse(WizardStateDto session) => new
    {
        sessionId = session.SessionId, tenantId = session.TenantId == Guid.Empty ? (Guid?)null : session.TenantId, currentStep = Math.Min(session.CurrentStep, 7), licenseMode = session.LicenseMode,
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
            var wizardState = await state.ResumeWizardAsync(SetupToken(), ct);
            if (wizardState.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before finance configuration.");
            var secretName = $"/tenants/{wizardState.TenantId:D}/finance/exchange-rates/OER_API_KEY";
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

    private static List<(string Table, string EntityType)> AutoMapTables(List<string> tables)
    {
        var result = new List<(string, string)>();
        foreach (var table in tables)
        {
            var lower = table.ToLowerInvariant();
            string? entityType = null;
            if (lower.Contains("student") || lower.Contains("learner"))
                entityType = "Student";
            else if (lower.Contains("facult") || lower.Contains("teacher") || lower.Contains("instructor") || lower.Contains("professor"))
                entityType = "Faculty";
            else if (lower.Contains("course") || lower.Contains("subject") || lower.Contains("class"))
                entityType = "Course";
            else if (lower.Contains("grade") || lower.Contains("mark") || lower.Contains("result") || lower.Contains("score"))
                entityType = "Grade";
            else if (lower.Contains("enroll") || lower.Contains("registr"))
                entityType = "Enrollment";
            else if (lower.Contains("fee") || lower.Contains("payment") || lower.Contains("invoice") || lower.Contains("bill"))
                entityType = "Fee";
            else if (lower.Contains("depart"))
                entityType = "Department";
            else if (lower.Contains("program") || lower.Contains("programme"))
                entityType = "Programme";
            if (entityType is not null)
                result.Add((table, entityType));
        }
        return result;
    }

    private static readonly Dictionary<string, string[]> ColumnToPropertyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FirstName"] = new[] { "first_name", "firstname", "fname", "given_name", "givenname", "forename", "name_first" },
        ["LastName"] = new[] { "last_name", "lastname", "lname", "surname", "family_name", "familyname", "name_last" },
        ["Email"] = new[] { "email", "email_address", "emailaddress", "e_mail", "mail" },
        ["PhoneNumber"] = new[] { "phone", "phone_number", "phonenumber", "mobile", "contact", "cell", "telephone", "tel" },
        ["DateOfBirth"] = new[] { "date_of_birth", "dob", "birthdate", "birth_date", "dateofbirth", "bday" },
        ["RegistrationNumber"] = new[] { "registration_number", "reg_number", "regno", "registration_no", "roll_number", "rollno", "student_number" },
        ["StudentId"] = new[] { "student_id", "studentid", "id", "user_id", "userid", "uid", "sid" },
        ["Code"] = new[] { "code", "course_code", "course_code", "subject_code", "class_code", "short_code", "shortcode" },
        ["Title"] = new[] { "title", "name", "course_name", "subject_name", "class_name", "full_name", "fullname", "display_name" },
        ["Description"] = new[] { "description", "desc", "details", "summary", "info", "notes" },
        ["UniversityId"] = new[] { "university_id", "universityid", "uni_id", "uniid", "matriculation_number", "matric", "matric_no", "enrollment_number" },
        ["FullName"] = new[] { "full_name", "fullname", "display_name", "displayname" },
    };

    private static Dictionary<string, string> AutoMapColumns(string entityType, List<ColumnInfo> columns)
    {
        var mappings = new Dictionary<string, string>();
        var usedSourceColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in ColumnToPropertyMap)
        {
            foreach (var col in columns)
            {
                if (usedSourceColumns.Contains(col.Name)) continue;
                if (kvp.Value.Contains(col.Name, StringComparer.OrdinalIgnoreCase))
                {
                    mappings[col.Name] = kvp.Key;
                    usedSourceColumns.Add(col.Name);
                    break;
                }
            }
        }

        foreach (var col in columns)
        {
            if (usedSourceColumns.Contains(col.Name)) continue;
            mappings[col.Name] = col.Name;
        }

        return mappings;
    }
}

public sealed record ValidateLicenseRequest(string LicenseKey);
public sealed record ChangeWizardModeRequest(Guid SessionId, string Mode, string? LicenseKey);
public sealed record SaveStepRequest(Guid SessionId, JsonElement Data);
public sealed record CompleteWizardRequest(Guid SessionId, string ConfirmedBy, DateTimeOffset Timestamp);
public sealed record DemoImportRequest(string ConnectionString, string DbType);
