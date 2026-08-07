using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using AegisCore.Domain.Entities;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardSessionService(AegisDbContext db, IPasswordHashingService passwords)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxStepPayloadBytes = 128 * 1024;

    public async Task<WizardSession> GetOrCreateAsync(Guid tenantId, string? ip, CancellationToken ct)
    {
        var session = await db.WizardSessions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.CompletedAt == null, ct);
        if (session is not null) return session;

        session = new WizardSession { Id = Guid.NewGuid(), TenantId = tenantId, CreatedByIp = ip };
        db.WizardSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public Task<WizardSession?> FindAsync(Guid tenantId, Guid sessionId, CancellationToken ct)
        => db.WizardSessions.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == sessionId && x.TenantId == tenantId, ct);

    public async Task<WizardSession> SaveAsync(Guid tenantId, Guid sessionId, int step, JsonElement data, CancellationToken ct)
    {
        if (step is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(step));
        if (Encoding.UTF8.GetByteCount(data.GetRawText()) > MaxStepPayloadBytes)
            throw new InvalidOperationException("Wizard step payload exceeds the 128 KiB limit.");
        var session = await db.WizardSessions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.TenantId == tenantId && x.CompletedAt == null, ct)
            ?? throw new KeyNotFoundException("Wizard session was not found.");
        if (step > session.CurrentStep + 1) throw new InvalidOperationException("Complete the previous wizard step first.");

        var root = JsonNode.Parse(session.StepData)?.AsObject() ?? new JsonObject();
        var storedData = JsonNode.Parse(data.GetRawText())?.AsObject() ?? new JsonObject();
        if (step == 6 && storedData["users"] is JsonArray users)
        {
            db.WizardAdminCredentials.RemoveRange(db.WizardAdminCredentials.Where(x => x.SessionId == sessionId && x.TenantId == tenantId));
            await db.SaveChangesAsync(ct);
            foreach (var user in users.OfType<JsonObject>())
            {
                var password = user["password"]?.GetValue<string>() ?? string.Empty;
                if (password.Length < 12) throw new InvalidOperationException("Administrator passwords must be at least 12 characters.");
                db.WizardAdminCredentials.Add(new WizardAdminCredential
                {
                    Id = Guid.NewGuid(), SessionId = sessionId, TenantId = tenantId,
                    FullName = user["name"]?.GetValue<string>()?.Trim() ?? string.Empty,
                    Email = user["email"]?.GetValue<string>()?.Trim().ToLowerInvariant() ?? string.Empty,
                    RoleName = user["role"]?.GetValue<string>()?.Trim() ?? string.Empty,
                    PasswordHash = passwords.HashPassword(password)
                });
                user.Remove("password");
            }
        }
        root[$"step{step}"] = storedData;
        session.StepData = root.ToJsonString(JsonOptions);
        session.CurrentStep = Math.Max(session.CurrentStep, step + 1);
        if (step == 0 && data.TryGetProperty("mode", out var mode)) session.LicenseMode = mode.GetString() ?? "demo";
        if (step == 0 && data.TryGetProperty("licenseKey", out var key) && key.ValueKind == JsonValueKind.String)
            session.LicenseKey = key.GetString();
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<WizardSession> ChangeModeAsync(Guid tenantId, Guid sessionId, string mode, string? licenseKey, CancellationToken ct)
    {
        var normalizedMode = mode.Trim().ToLowerInvariant() switch
        {
            "demo" => "demo",
            "paid" or "live" => "paid",
            _ => throw new ArgumentException("Mode must be demo or live.", nameof(mode))
        };
        var session = await db.WizardSessions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.TenantId == tenantId && x.CompletedAt == null, ct)
            ?? throw new KeyNotFoundException("Wizard session was not found.");

        session.LicenseMode = normalizedMode;
        session.LicenseKey = normalizedMode == "paid" ? licenseKey?.Trim() : null;
        var root = JsonNode.Parse(session.StepData)?.AsObject() ?? new JsonObject();
        var step0 = root["step0"]?.AsObject() ?? new JsonObject();
        step0["mode"] = normalizedMode;
        if (normalizedMode == "paid")
        {
            step0["licenseKey"] = session.LicenseKey;
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
        session.StepData = root.ToJsonString(JsonOptions);
        session.CurrentStep = Math.Max(session.CurrentStep, 1);
        if (normalizedMode == "paid" && session.CurrentStep > 3)
            session.CurrentStep = 3;
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task MarkGatewayConfiguredAsync(Guid tenantId, Guid sessionId, string gatewaySlug, CancellationToken ct)
    {
        var session = await db.WizardSessions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.TenantId == tenantId && x.CompletedAt == null, ct)
            ?? throw new KeyNotFoundException("Wizard session was not found.");
        var root = JsonNode.Parse(session.StepData)?.AsObject() ?? new JsonObject();
        var step = root["step3"]?.AsObject() ?? new JsonObject();
        var configured = step["configuredGateways"] as JsonArray ?? [];
        if (!configured.Any(x => string.Equals(x?.GetValue<string>(), gatewaySlug, StringComparison.OrdinalIgnoreCase))) configured.Add(gatewaySlug);
        step["configuredGateways"] = configured;
        root["step3"] = step;
        session.StepData = root.ToJsonString(JsonOptions);
        await db.SaveChangesAsync(ct);
    }

    public async Task<WizardSession> CompleteAsync(Guid tenantId, Guid sessionId, string confirmedBy, CancellationToken ct)
    {
        var session = await db.WizardSessions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.TenantId == tenantId && x.CompletedAt == null, ct)
            ?? throw new KeyNotFoundException("Wizard session was not found.");
        if (session.CurrentStep < 7) throw new InvalidOperationException("All wizard steps must be saved before activation.");
        var root = JsonNode.Parse(session.StepData)?.AsObject() ?? new JsonObject();
        var review = root["step7"]?.AsObject();
        if (review?["confirmed"]?.GetValue<bool>() != true) throw new InvalidOperationException("Both legal confirmations are required.");
        var users = root["step6"]?["users"]?.AsArray() ?? [];
        var confirmedEmail = users.Select(x => x?["email"]?.GetValue<string>()).FirstOrDefault(x => string.Equals(x, confirmedBy, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(confirmedEmail)) throw new InvalidOperationException("The activating administrator must be one of the configured administrators.");
        var credentials = await db.WizardAdminCredentials.IgnoreQueryFilters()
            .Where(x => x.SessionId == sessionId && x.TenantId == tenantId).ToListAsync(ct);
        if (!credentials.Any(x => x.RoleName == "Super Admin") || !credentials.Any(x => x.RoleName == "Finance Controller"))
            throw new InvalidOperationException("A Super Admin and Finance Controller are required.");
        foreach (var credential in credentials)
        {
            var user = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Email == credential.Email, ct);
            if (user is null)
            {
                var parts = credential.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                user = new User { Id = Guid.NewGuid(), TenantId = tenantId, Username = credential.Email, Email = credential.Email, FirstName = parts.ElementAtOrDefault(0) ?? credential.FullName, LastName = parts.ElementAtOrDefault(1) ?? string.Empty };
                db.Users.Add(user);
            }
            user.PasswordHash = credential.PasswordHash;
            var role = await db.Roles.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Name == credential.RoleName, ct);
            if (role is null)
            {
                role = new Role { Id = Guid.NewGuid(), TenantId = tenantId, Name = credential.RoleName, IsSystem = true, Description = "Created by the AegisCore setup wizard." };
                db.Roles.Add(role);
            }
            if (!await db.UserRoles.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.UserId == user.Id && x.RoleId == role.Id, ct))
                db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), TenantId = tenantId, UserId = user.Id, RoleId = role.Id, AssignedByUserId = user.Id });
        }
        session.CompletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return session;
    }
}
