using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Onboarding;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardSessionService(
    AegisDbContext db,
    IPasswordHashingService passwords,
    WizardStateService states,
    IConfiguration? configuration = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxStepPayloadBytes = 128 * 1024;

    public async Task<WizardStateDto> GetOrCreateAsync(string token, string? ip, CancellationToken ct)
    {
        var state = await states.GetOrCreateStateAsync(token, ip, ct);
        if (Guid.TryParse(configuration?["TENANT_ID"], out var installationTenant) && installationTenant != Guid.Empty)
        {
            if (state.TenantId != Guid.Empty && state.TenantId != installationTenant)
                throw new InvalidOperationException("Wizard tenant does not match this installation. Restore the matching configuration.");
            if (state.TenantId == Guid.Empty)
            {
                state.TenantId = installationTenant;
                await states.SaveState(token, state, ct);
            }
        }
        return state;
    }

    public async Task<WizardStateDto?> FindAsync(string token, Guid sessionId, CancellationToken ct)
    {
        try
        {
            var state = await states.LoadState(token, ct);
            return state.SessionId == sessionId ? state : null;
        }
        catch (WizardSessionNotFoundException)
        {
            return null;
        }
    }

    public async Task<WizardStateDto> SaveAsync(string token, Guid sessionId, int step, JsonElement data, CancellationToken ct)
    {
        if (step is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(step));
        if (Encoding.UTF8.GetByteCount(data.GetRawText()) > MaxStepPayloadBytes)
            throw new InvalidOperationException("Wizard step payload exceeds the 128 KiB limit.");

        var state = step == 1
            ? await states.InitializeTenantAtStepOneAsync(token, ct)
            : await states.ResumeWizardAsync(token, ct);
        EnsureSession(state, sessionId);
        if (step > state.CurrentStep + 1) throw new InvalidOperationException("Complete the previous wizard step first.");

        var root = JsonNode.Parse(state.StepData)?.AsObject() ?? new JsonObject();
        var storedData = JsonNode.Parse(data.GetRawText())?.AsObject() ?? new JsonObject();
        if (step == 7 && storedData["users"] is JsonArray users)
        {
            EnsureTenant(state);
            state.AdminPassword = string.Empty;
            db.WizardAdminCredentials.RemoveRange(db.WizardAdminCredentials.IgnoreQueryFilters()
                .Where(item => item.SessionId == sessionId && item.TenantId == state.TenantId));
            await db.SaveChangesAsync(ct);
            foreach (var user in users.OfType<JsonObject>())
            {
                var password = user["password"]?.GetValue<string>() ?? string.Empty;
                if (password.Length < 12) throw new InvalidOperationException("Administrator passwords must be at least 12 characters.");
                if (string.IsNullOrEmpty(state.AdminPassword)) state.AdminPassword = password;
                db.WizardAdminCredentials.Add(new WizardAdminCredential
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    TenantId = state.TenantId,
                    FullName = user["name"]?.GetValue<string>()?.Trim() ?? string.Empty,
                    Email = user["email"]?.GetValue<string>()?.Trim().ToLowerInvariant() ?? string.Empty,
                    RoleName = user["role"]?.GetValue<string>()?.Trim() ?? string.Empty,
                    PasswordHash = passwords.HashPassword(password)
                });
                user.Remove("password");
            }
            var firstAdministrator = users.OfType<JsonObject>().FirstOrDefault();
            state.AdminFullName = firstAdministrator?["name"]?.GetValue<string>()?.Trim() ?? string.Empty;
            state.AdminAccountEmail = firstAdministrator?["email"]?.GetValue<string>()?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        root[$"step{step}"] = storedData;
        state.StepData = root.ToJsonString(JsonOptions);
        state.CurrentStep = Math.Max(state.CurrentStep, step + 1);
        if (step == 0 && data.TryGetProperty("mode", out var mode)) state.LicenseMode = mode.GetString() ?? "demo";
        if (step == 0 && data.TryGetProperty("licenseKey", out var key) && key.ValueKind == JsonValueKind.String)
            state.LicenseKey = key.GetString() ?? string.Empty;
        await states.SaveState(token, state, ct);
        return state;
    }

    public async Task<WizardStateDto> ChangeModeAsync(string token, Guid sessionId, string mode, string? licenseKey, CancellationToken ct)
    {
        var normalizedMode = mode.Trim().ToLowerInvariant() switch
        {
            "demo" => "demo",
            "paid" or "live" => "paid",
            _ => throw new ArgumentException("Mode must be demo or live.", nameof(mode))
        };
        var state = await states.ResumeWizardAsync(token, ct);
        EnsureSession(state, sessionId);

        state.LicenseMode = normalizedMode;
        state.LicenseKey = normalizedMode == "paid" ? licenseKey?.Trim() ?? string.Empty : string.Empty;
        var root = JsonNode.Parse(state.StepData)?.AsObject() ?? new JsonObject();
        var step0 = root["step0"]?.AsObject() ?? new JsonObject();
        step0["mode"] = normalizedMode;
        if (normalizedMode == "paid")
        {
            step0["licenseKey"] = state.LicenseKey;
            step0["licenseValidated"] = true;
        }
        else
        {
            step0.Remove("licenseKey");
            step0.Remove("licenseValidated");
            var step3 = root["step3"]?.AsObject() ?? new JsonObject();
            step3["enabledGateways"] = new JsonArray();
            step3["configuredGateways"] = new JsonArray();
            root["step3"] = step3;
        }
        root["step0"] = step0;
        state.StepData = root.ToJsonString(JsonOptions);
        state.CurrentStep = Math.Max(state.CurrentStep, 1);
        if (normalizedMode == "paid" && state.CurrentStep > 3) state.CurrentStep = 3;
        await states.SaveState(token, state, ct);
        return state;
    }

    public async Task MarkGatewayConfiguredAsync(string token, Guid sessionId, string gatewaySlug, CancellationToken ct)
    {
        var state = await states.ResumeWizardAsync(token, ct);
        EnsureSession(state, sessionId);
        var root = JsonNode.Parse(state.StepData)?.AsObject() ?? new JsonObject();
        var step = root["step3"]?.AsObject() ?? new JsonObject();
        var configured = step["configuredGateways"] as JsonArray ?? [];
        if (!configured.Any(item => string.Equals(item?.GetValue<string>(), gatewaySlug, StringComparison.OrdinalIgnoreCase)))
            configured.Add(gatewaySlug);
        step["configuredGateways"] = configured;
        root["step3"] = step;
        state.StepData = root.ToJsonString(JsonOptions);
        await states.SaveState(token, state, ct);
    }

    public async Task<WizardStateDto> CompleteAsync(string token, Guid sessionId, string confirmedBy, CancellationToken ct)
    {
        var state = await states.ResumeWizardAsync(token, ct);
        EnsureSession(state, sessionId);
        EnsureTenant(state);
        if (state.CurrentStep < 7) throw new InvalidOperationException("All wizard steps must be saved before activation.");
        var root = JsonNode.Parse(state.StepData)?.AsObject() ?? new JsonObject();
        var review = root["step7"]?.AsObject();
        if (review?["confirmed"]?.GetValue<bool>() != true) throw new InvalidOperationException("Both legal confirmations are required.");
        var users = root["step7"]?["users"]?.AsArray() ?? [];
        var confirmedEmail = users.Select(item => item?["email"]?.GetValue<string>())
            .FirstOrDefault(email => string.Equals(email, confirmedBy, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(confirmedEmail))
            throw new InvalidOperationException("The activating administrator must be one of the configured administrators.");

        var credentials = await db.WizardAdminCredentials.IgnoreQueryFilters()
            .Where(item => item.SessionId == sessionId && item.TenantId == state.TenantId).ToListAsync(ct);
        if (credentials.Count == 0 || credentials.Any(item => string.IsNullOrWhiteSpace(item.RoleName)))
            throw new InvalidOperationException("At least one administrator assigned to a configured role is required.");

        foreach (var credential in credentials)
        {
            var user = await db.Users.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.TenantId == state.TenantId && item.Email == credential.Email, ct);
            if (user is null)
            {
                var parts = credential.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                user = new User
                {
                    Id = Guid.NewGuid(), TenantId = state.TenantId, Username = credential.Email, Email = credential.Email,
                    FirstName = parts.ElementAtOrDefault(0) ?? credential.FullName,
                    LastName = parts.ElementAtOrDefault(1) ?? string.Empty
                };
                db.Users.Add(user);
            }
            user.PasswordHash = credential.PasswordHash;
            var role = await db.Roles.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.TenantId == state.TenantId && item.Name == credential.RoleName, ct);
            if (role is null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(), TenantId = state.TenantId, Name = credential.RoleName, IsSystem = true,
                    Description = "Created by the AegisCore setup wizard."
                };
                db.Roles.Add(role);
            }
            if (!await db.UserRoles.IgnoreQueryFilters().AnyAsync(
                    item => item.TenantId == state.TenantId && item.UserId == user.Id && item.RoleId == role.Id, ct))
                db.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(), TenantId = state.TenantId, UserId = user.Id, RoleId = role.Id, AssignedByUserId = user.Id
                });
        }

        state.CompletedAt = DateTimeOffset.UtcNow;
        await states.SaveState(token, state, ct);
        return state;
    }

    private static void EnsureSession(WizardStateDto state, Guid sessionId)
    {
        if (state.SessionId != sessionId) throw new WizardSessionNotFoundException();
    }

    private static void EnsureTenant(WizardStateDto state)
    {
        if (state.TenantId == Guid.Empty) throw new InvalidOperationException("Complete wizard Step 1 before continuing.");
    }
}
