using System.Text.Json;
using System.Text.Json.Nodes;
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
    WizardProvisioningService provisioning,
    WizardStateService state,
    OnboardingService onboarding,
    IInstallationStateService installation) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

    [HttpPost("step/{step:int}/save")]
    public async Task<IActionResult> SaveStep(int step, [FromBody] SaveStepRequest request, CancellationToken ct)
    {
        if (step is < 0 or > 7) return BadRequest(new { success = false, error = "Invalid wizard step." });
        if (!ValidateStep(step, request.Data, out var error)) return UnprocessableEntity(new { success = false, error });
        if (step == 0 && request.Data.TryGetProperty("mode", out var mode) && mode.GetString() == "paid")
        {
            var licenseKey = request.Data.GetProperty("licenseKey").GetString() ?? string.Empty;
            var license = await onboarding.ValidateLicenseAsync(state, licenseKey, ct);
            if (!license.IsValid) return UnprocessableEntity(new { success = false, error = license.Error?.UserMessage ?? "License validation failed." });
        }
        var data = step == 3 ? SanitizeGatewayStep(request.Data) : request.Data;
        var session = await sessions.SaveAsync(TenantId(), request.SessionId, step, data, ct);
        return Ok(new { success = true, nextStep = Math.Min(session.CurrentStep, 7), sessionId = session.Id });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteWizardRequest request, CancellationToken ct)
    {
        try
        {
            var session = await sessions.GetOrCreateAsync(TenantId(), HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            if (session.Id != request.SessionId) return UnprocessableEntity(new { success = false, error = "Wizard session was not found." });
            await provisioning.ProvisionAsync(TenantId(), session.StepData, ct);
            await sessions.CompleteAsync(TenantId(), request.SessionId, request.ConfirmedBy, ct);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { success = false, error = ex.Message });
        }
        await installation.MarkAsInstalledAsync(ct);
        return Ok(new { success = true, tenantId = TenantId(), dashboardUrl = "/", adminEmail = request.ConfirmedBy });
    }

    private Guid TenantId() => Guid.TryParse(configuration["TENANT_ID"], out var configured) ? configured : state.TenantId;

    private static object ToResponse(AegisCore.Domain.Entities.WizardSession session) => new
    {
        sessionId = session.Id, currentStep = Math.Min(session.CurrentStep, 7), licenseMode = session.LicenseMode,
        completedAt = session.CompletedAt, data = JsonNode.Parse(session.StepData) ?? new JsonObject()
    };

    private static JsonElement SanitizeGatewayStep(JsonElement data)
    {
        var output = new JsonObject();
        foreach (var name in new[] { "enabledGateways", "configuredGateways", "mode" })
            if (data.TryGetProperty(name, out var value)) output[name] = JsonNode.Parse(value.GetRawText());
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
            case 2 when !Has("academicYear", "calendarType", "semesterStart", "semesterEnd"):
                error = "Complete the academic calendar."; return false;
            case 2 when !HasArray(data, "faculties") || !HasArray(data, "departments") || !HasArray(data, "programmes"):
                error = "Add at least one faculty, department, and programme."; return false;
            case 4 when !Has("standard", "fiscalYear", "baseCurrency", "rateSource"):
                error = "Complete the finance configuration."; return false;
            case 5 when !HasArray(data, "feeHeads") || !HasArray(data, "assignments"):
                error = "Define fee heads and at least one programme assignment."; return false;
            case 6 when !HasArray(data, "users"):
                error = "Create the initial administrator accounts."; return false;
            case 7 when !data.TryGetProperty("confirmed", out var confirmed) || confirmed.ValueKind != JsonValueKind.True:
                error = "Both legal confirmations are required."; return false;
        }
        return true;
    }

    private static bool HasArray(JsonElement data, string name) => data.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0;
}

public sealed record ValidateLicenseRequest(string LicenseKey);
public sealed record SaveStepRequest(Guid SessionId, JsonElement Data);
public sealed record CompleteWizardRequest(Guid SessionId, string ConfirmedBy, DateTimeOffset Timestamp);
