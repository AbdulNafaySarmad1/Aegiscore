using System.Text.Json;
using System.Text.Json.Nodes;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardGatewayService(AegisDbContext db, ISecretService secrets)
{
    private static readonly IReadOnlyDictionary<string, string[]> RequiredFields = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["raast"] = ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "IBAN", "WEBHOOK_SECRET", "ENV"],
        ["stripe"] = ["PUBLISHABLE_KEY", "SECRET_KEY", "WEBHOOK_SECRET", "ACCOUNT_ID"],
        ["jazzcash"] = ["MERCHANT_ID", "PASSWORD", "INTEGRITY_SALT", "WEBHOOK_SECRET"],
        ["easypaisa"] = ["STORE_ID", "PASSWORD", "WEBHOOK_SECRET"],
        ["gocardless"] = ["ACCESS_TOKEN", "WEBHOOK_SECRET"],
        ["1link"] = ["MERCHANT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"]
    };

    public async Task<GatewayConfigurationResult> ConfigureAsync(Guid tenantId, string slug, JsonElement payload, CancellationToken ct)
    {
        var normalized = NormalizeSlug(slug);
        if (!RequiredFields.TryGetValue(normalized, out var required)) throw new InvalidOperationException("Unsupported payment gateway.");
        if (payload.ValueKind != JsonValueKind.Object) throw new ArgumentException("Gateway credentials must be an object.");

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in payload.EnumerateObject())
        {
            var key = property.Name.Trim().ToUpperInvariant();
            if (!key.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_') || key.Length > 60)
                throw new ArgumentException("Gateway credential field name is invalid.");
            if (property.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.Value.GetString()))
                throw new ArgumentException("Gateway credential values must be non-empty strings.");
            fields[key] = property.Value.GetString()!;
        }

        var missing = required.Where(name => !fields.ContainsKey(name)).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException($"Required gateway fields are missing: {string.Join(", ", missing)}.");
        if (fields.TryGetValue("ENV", out var environment) && environment is not ("sandbox" or "production"))
            throw new ArgumentException("Gateway environment is invalid.");

        var secretNamespace = $"/tenants/{tenantId:D}/gateways/{normalized}";
        foreach (var field in fields)
            await secrets.SetSecretAsync($"{secretNamespace}/{field.Key}", field.Value, ct);

        var configuration = await db.PaymentGatewayConfigurations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.GatewaySlug == normalized, ct);
        if (configuration is null)
        {
            configuration = new PaymentGatewayConfiguration { Id = Guid.NewGuid(), TenantId = tenantId, GatewaySlug = normalized };
            db.PaymentGatewayConfigurations.Add(configuration);
        }
        configuration.SecretNamespace = secretNamespace;
        configuration.Environment = fields.TryGetValue("ENV", out var env) ? env : "production";
        configuration.IsActive = true;
        configuration.HealthStatus = "configured";
        configuration.HealthError = null;
        configuration.LastHealthCheckAt = DateTime.UtcNow;
        configuration.ConfiguredAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new GatewayConfigurationResult(normalized, true, configuration.HealthStatus, configuration.LastHealthCheckAt);
    }

    public async Task<GatewayHealthResult> HealthCheckAsync(Guid tenantId, string slug, CancellationToken ct)
    {
        var normalized = NormalizeSlug(slug);
        if (!RequiredFields.TryGetValue(normalized, out var required)) throw new InvalidOperationException("Unsupported payment gateway.");
        var configuration = await db.PaymentGatewayConfigurations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.GatewaySlug == normalized, ct)
            ?? throw new InvalidOperationException("Gateway is not configured.");
        var missing = new List<string>();
        foreach (var field in required)
            if (!await secrets.SecretExistsAsync($"{configuration.SecretNamespace}/{field}", ct)) missing.Add(field);
        configuration.LastHealthCheckAt = DateTime.UtcNow;
        configuration.HealthStatus = missing.Count == 0 ? "ok" : "error";
        configuration.HealthError = missing.Count == 0 ? null : "Required gateway secrets are unavailable.";
        await db.SaveChangesAsync(ct);
        return new GatewayHealthResult(normalized, missing.Count == 0, configuration.HealthStatus, configuration.LastHealthCheckAt, missing);
    }

    private static string NormalizeSlug(string slug)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length > 30 || !normalized.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')) throw new ArgumentException("Gateway slug is invalid.");
        return normalized;
    }
}

public sealed record GatewayConfigurationResult(string GatewaySlug, bool Success, string HealthStatus, DateTime? LastHealthCheckAt);
public sealed record GatewayHealthResult(string GatewaySlug, bool Success, string HealthStatus, DateTime? LastHealthCheckAt, IReadOnlyList<string> MissingFields);
