using System.Text.Json;
using System.Text.Json.Nodes;
using AegisCore.Domain.Entities;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardSessionService(AegisDbContext db, IPasswordHashingService passwords)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

    public async Task<WizardSession> SaveAsync(Guid tenantId, Guid sessionId, int step, JsonElement data, CancellationToken ct)
    {
        if (step is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(step));
        var session = await db.WizardSessions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.TenantId == tenantId && x.CompletedAt == null, ct)
            ?? throw new KeyNotFoundException("Wizard session was not found.");
        if (step > session.CurrentStep + 1) throw new InvalidOperationException("Complete the previous wizard step first.");

        var root = JsonNode.Parse(session.StepData)?.AsObject() ?? new JsonObject();
        var storedData = JsonNode.Parse(data.GetRawText())?.AsObject() ?? new JsonObject();
        if (step == 6 && storedData["users"] is JsonArray users)
        {
            db.WizardAdminCredentials.RemoveRange(db.WizardAdminCredentials.Where(x => x.SessionId == sessionId && x.TenantId == tenantId));
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
                role = new Role { Id = Guid.NewGuid(), TenantId = tenantId, Name = credential.RoleName, IsSystem = true, Description = "Created by the Nocturne setup wizard." };
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
