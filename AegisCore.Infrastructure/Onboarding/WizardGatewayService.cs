using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class WizardGatewayService(AegisDbContext db, ISecretService secrets)
{
    private static readonly IReadOnlyDictionary<string, string[]> RequiredFields = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["raast"] = ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "IBAN", "WEBHOOK_SECRET", "ENV"],
        // PayFast (Pakistan) hosted checkout. No WEBHOOK_SECRET: its IPN is authenticated by a hash keyed on SECURED_KEY.
        ["payfast"] = ["MERCHANT_ID", "SECURED_KEY", "MERCHANT_NAME", "ENV"],
        ["stripe"] = ["PUBLISHABLE_KEY", "SECRET_KEY", "WEBHOOK_SECRET", "ACCOUNT_ID"],
        ["jazzcash"] = ["MERCHANT_ID", "PASSWORD", "INTEGRITY_SALT", "WEBHOOK_SECRET"],
        ["easypaisa"] = ["STORE_ID", "PASSWORD", "WEBHOOK_SECRET"],
        ["gocardless"] = ["ACCESS_TOKEN", "WEBHOOK_SECRET"],
        ["1link"] = ["MERCHANT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
        ["jazzcash-merchant"] = ["MERCHANT_ID", "PASSWORD", "INTEGRITY_SALT", "WEBHOOK_SECRET"],
        ["hbl-pay"] = ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
        ["mcb-corporate"] = ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
        ["ubl-omni"] = ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
        ["network-international"] = ["MERCHANT_ID", "API_KEY", "API_SECRET", "WEBHOOK_SECRET"],
        ["telr"] = ["STORE_ID", "AUTH_KEY", "WEBHOOK_SECRET"],
        ["plaid-ach"] = ["CLIENT_ID", "SECRET_KEY", "WEBHOOK_SECRET"],
        ["billplz-fpx"] = ["API_KEY", "X_SIGNATURE", "WEBHOOK_SECRET"],
        ["duitnow-qr"] = ["MERCHANT_ID", "CLIENT_ID", "CLIENT_SECRET", "WEBHOOK_SECRET"],
        ["swift-mt103"] = ["BIC", "SIGNING_KEY_ID"]
    };

    // Gateways whose callbacks cannot carry an ML-DSA signature because the provider defines its own
    // authentication. PayFast signs its IPN with a SHA-256 hash keyed on the merchant's secured key and
    // AegisCore re-reads every success from PayFast's status API (see PayFastCheckoutService), so it does
    // not go through the generic /api/webhooks receiver the ML-DSA rule protects.
    private static readonly HashSet<string> HashAuthenticatedGateways = new(StringComparer.OrdinalIgnoreCase) { "payfast" };

    public async Task<GatewayConfigurationResult> ConfigureAsync(Guid tenantId, string slug, JsonElement payload, CancellationToken ct, Guid configuredByUserId = default, bool demoMode = false)
    {
        var normalized = NormalizeSlug(slug);
        if (!RequiredFields.TryGetValue(normalized, out var required)) throw new InvalidOperationException("Unsupported payment gateway.");
        if (payload.ValueKind != JsonValueKind.Object) throw new ArgumentException("Gateway credentials must be an object.");

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in payload.EnumerateObject())
        {
            if (fields.Count >= 32) throw new ArgumentException("Gateway credential payload contains too many fields.");
            var key = property.Name.Trim().ToUpperInvariant();
            if (!key.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_') || key.Length is 0 or > 60)
                throw new ArgumentException("Gateway credential field name is invalid.");
            var value = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            if (string.IsNullOrWhiteSpace(value) || value.Length > 4096)
                throw new ArgumentException("Gateway credential values must be non-empty strings.");
            fields[key] = value;
        }

        if (!fields.TryGetValue("ENV", out var environment))
            throw new InvalidOperationException("Gateway environment must be selected explicitly.");
        environment = environment.Trim().ToLowerInvariant();
        if (environment is not ("sandbox" or "production"))
            throw new ArgumentException("Gateway environment is invalid.");
        if (demoMode && environment != "sandbox")
            throw new ArgumentException("Demo mode only supports sandbox gateway configuration.");
        if (demoMode)
        {
            foreach (var field in required)
                fields.TryAdd(field, $"demo-{normalized}-{field.ToLowerInvariant()}");
            fields["ENV"] = "sandbox";
            environment = "sandbox";
        }

        var missing = required.Where(name => !fields.ContainsKey(name)).ToList();
        if (missing.Count > 0) throw new InvalidOperationException($"Required gateway fields are missing: {string.Join(", ", missing)}.");
        if (environment == "production" && !HashAuthenticatedGateways.Contains(normalized))
        {
            if (!fields.TryGetValue("PQC_ALGORITHM", out var algorithm) || !string.Equals(algorithm.Trim(), "ML-DSA-65", StringComparison.Ordinal))
                throw new ArgumentException("Production gateways require an ML-DSA-65 webhook verification key.");
            if (!fields.TryGetValue("PQC_PUBLIC_KEY", out var publicKey))
                throw new ArgumentException("Production gateways require an ML-DSA public key.");
            try { _ = Convert.FromBase64String(publicKey); }
            catch (FormatException) { throw new ArgumentException("Production gateway ML-DSA public key must be base64 encoded."); }
        }

        var secretNamespace = $"/tenants/{tenantId:D}/gateways/{normalized}";
        foreach (var field in fields)
            await secrets.SetSecretAsync($"{secretNamespace}/{field.Key}", field.Value, ct);

        // Intentional setup bypass: install mode has no authenticated tenant context; the query is constrained by the bootstrap session or explicit tenant predicate.
        var configuration = await db.PaymentGatewayConfigurations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.GatewaySlug == normalized, ct);
        if (configuration is null)
        {
            configuration = new PaymentGatewayConfiguration { Id = Guid.NewGuid(), TenantId = tenantId, GatewaySlug = normalized };
            db.PaymentGatewayConfigurations.Add(configuration);
        }
        configuration.SecretNamespace = secretNamespace;
        configuration.Environment = environment;
        configuration.IsActive = true;
        configuration.HealthStatus = "configured";
        configuration.HealthError = null;
        configuration.LastHealthCheckAt = null;
        if (configuredByUserId != Guid.Empty) configuration.ConfiguredByUserId = configuredByUserId;
        configuration.ConfiguredAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var health = await HealthCheckAsync(tenantId, normalized, ct);
        return new GatewayConfigurationResult(normalized, health.Success, health.HealthStatus, health.LastHealthCheckAt, health.LatencyMs);
    }

    public async Task<GatewayHealthResult> HealthCheckAsync(Guid tenantId, string slug, CancellationToken ct)
    {
        var normalized = NormalizeSlug(slug);
        if (!RequiredFields.TryGetValue(normalized, out var required)) throw new InvalidOperationException("Unsupported payment gateway.");
        // Intentional setup bypass: install mode has no authenticated tenant context; the query is constrained by the bootstrap session or explicit tenant predicate.
        var configuration = await db.PaymentGatewayConfigurations.IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.GatewaySlug == normalized, ct)
            ?? throw new InvalidOperationException("Gateway is not configured.");
        var expectedNamespace = $"/tenants/{tenantId:D}/gateways/{normalized}";
        if (!string.Equals(configuration.SecretNamespace, expectedNamespace, StringComparison.Ordinal))
            throw new InvalidOperationException("Gateway secret namespace is invalid.");
        var stopwatch = Stopwatch.StartNew();
        var healthFields = configuration.Environment.Equals("production", StringComparison.OrdinalIgnoreCase) && !HashAuthenticatedGateways.Contains(normalized)
            ? required.Concat(["PQC_ALGORITHM", "PQC_PUBLIC_KEY"]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : required;
        var availability = await Task.WhenAll(healthFields.Select(async field =>
            (Field: field, Exists: await SecretAvailableAsync($"{configuration.SecretNamespace}/{field}", ct))));
        var missing = availability.Where(x => !x.Exists).Select(x => x.Field).ToList();
        stopwatch.Stop();
        configuration.LastHealthCheckAt = DateTime.UtcNow;
        configuration.HealthStatus = missing.Count == 0 ? "ok" : "error";
        configuration.HealthError = missing.Count == 0 ? null : "Required gateway secrets are unavailable.";
        await db.SaveChangesAsync(ct);
        return new GatewayHealthResult(normalized, missing.Count == 0, configuration.HealthStatus, configuration.LastHealthCheckAt, missing, stopwatch.ElapsedMilliseconds);
    }

    private async Task<bool> SecretAvailableAsync(string secretName, CancellationToken ct)
    {
        try { return await secrets.SecretExistsAsync(secretName, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { return false; }
    }

    public async Task DisableLiveConfigurationAsync(Guid tenantId, CancellationToken ct)
    {
        // Intentional setup bypass: install mode has no authenticated tenant context; the query is constrained by the bootstrap session or explicit tenant predicate.
        var configurations = await db.PaymentGatewayConfigurations.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(ct);

        foreach (var configuration in configurations)
        {
            if (!RequiredFields.TryGetValue(configuration.GatewaySlug, out var fields)) continue;
            var secretNamespace = $"/tenants/{tenantId:D}/gateways/{configuration.GatewaySlug}";
            var allFields = fields.Concat(["PQC_ALGORITHM", "PQC_PUBLIC_KEY"]).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var field in allFields)
                await secrets.DeleteSecretAsync($"{secretNamespace}/{field}", ct);

            configuration.IsActive = false;
            configuration.HealthStatus = "disabled";
            configuration.HealthError = "Disabled because this tenant is running in demo mode.";
            configuration.LastHealthCheckAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private static string NormalizeSlug(string slug)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length > 30 || !normalized.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')) throw new ArgumentException("Gateway slug is invalid.");
        return normalized;
    }
}

public sealed record GatewayConfigurationResult(string GatewaySlug, bool Success, string HealthStatus, DateTime? LastHealthCheckAt, long LatencyMs = 0);
public sealed record GatewayHealthResult(string GatewaySlug, bool Success, string HealthStatus, DateTime? LastHealthCheckAt, IReadOnlyList<string> MissingFields, long LatencyMs = 0);
