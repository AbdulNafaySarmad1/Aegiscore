using System.Text.Json;
using AegisCore.Application.Onboarding;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class EfWizardStateStore : IWizardStateStore
{
    private const string SecretReferencePrefix = "secret://";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AegisDbContext _db;
    private readonly ISecretService _secrets;
    private readonly IDataProtector _protector;

    public EfWizardStateStore(AegisDbContext db, ISecretService secrets, IDataProtectionProvider protectionProvider)
    {
        _db = db;
        _secrets = secrets;
        _protector = protectionProvider.CreateProtector("AegisCore.WizardState.Payload.v1");
    }

    public async Task<WizardStateDto?> LoadAsync(string bootstrapTokenHash, CancellationToken ct = default)
    {
        var session = await _db.WizardSessions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.BootstrapTokenHash == bootstrapTokenHash, ct);
        if (session is null) return null;

        WizardStateDto state;
        if (string.IsNullOrWhiteSpace(session.Payload))
        {
            state = new WizardStateDto
            {
                SessionId = session.Id,
                TenantId = session.TenantId,
                CurrentStep = session.CurrentStep,
                LicenseMode = session.LicenseMode,
                LicenseKey = session.LicenseKey ?? string.Empty,
                StepData = session.StepData,
                StartedAt = session.StartedAt,
                CompletedAt = session.CompletedAt,
                CreatedByIp = session.CreatedByIp
            };
        }
        else
        {
            var json = _protector.Unprotect(session.Payload);
            state = JsonSerializer.Deserialize<WizardStateDto>(json, JsonOptions)
                ?? throw new InvalidOperationException("The persisted setup state is invalid.");
        }

        state.SessionId = session.Id;
        state.TenantId = session.TenantId;
        state.CurrentStep = session.CurrentStep;
        state.LicenseMode = session.LicenseMode;
        state.StartedAt = session.StartedAt;
        state.CompletedAt = session.CompletedAt;
        state.CreatedByIp = session.CreatedByIp;
        await RestoreSensitiveValuesAsync(state, ct);
        return state;
    }

    public async Task SaveAsync(string bootstrapTokenHash, WizardStateDto state, CancellationToken ct = default)
    {
        var session = await _db.WizardSessions.IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.BootstrapTokenHash == bootstrapTokenHash, ct);
        if (session is null)
        {
            session = new WizardSession
            {
                Id = state.SessionId == Guid.Empty ? Guid.NewGuid() : state.SessionId,
                BootstrapTokenHash = bootstrapTokenHash,
                StartedAt = state.StartedAt == default ? DateTimeOffset.UtcNow : state.StartedAt
            };
            _db.WizardSessions.Add(session);
        }

        state.SessionId = session.Id;
        state.StartedAt = session.StartedAt;
        var persisted = Clone(state);
        await ProtectSensitiveValuesAsync(bootstrapTokenHash, persisted, ct);

        session.TenantId = state.TenantId;
        session.CurrentStep = state.CurrentStep;
        session.LicenseMode = state.LicenseMode;
        session.LicenseKey = null;
        session.StepData = "{}";
        session.CompletedAt = state.CompletedAt;
        session.CreatedByIp = state.CreatedByIp;
        session.Payload = _protector.Protect(JsonSerializer.Serialize(persisted, JsonOptions));
        await _db.SaveChangesAsync(ct);
    }

    private async Task ProtectSensitiveValuesAsync(string tokenHash, WizardStateDto state, CancellationToken ct)
    {
        state.LicenseKey = await StoreSecretAsync(tokenHash, "license-key", state.LicenseKey, ct) ?? string.Empty;
        state.DbPassword = await StoreSecretAsync(tokenHash, "database-password", state.DbPassword, ct) ?? string.Empty;
        state.RedisPassword = await StoreSecretAsync(tokenHash, "redis-password", state.RedisPassword, ct);
        state.MinioSecretKey = await StoreSecretAsync(tokenHash, "minio-secret-key", state.MinioSecretKey, ct);
        state.AzureClientSecret = await StoreSecretAsync(tokenHash, "azure-client-secret", state.AzureClientSecret, ct);
        state.SmtpPassword = await StoreSecretAsync(tokenHash, "smtp-password", state.SmtpPassword, ct) ?? string.Empty;
        state.CloudflareTunnelToken = await StoreSecretAsync(tokenHash, "cloudflare-tunnel-token", state.CloudflareTunnelToken, ct);
        state.AdminPassword = await StoreSecretAsync(tokenHash, "administrator-password", state.AdminPassword, ct) ?? string.Empty;
    }

    private async Task RestoreSensitiveValuesAsync(WizardStateDto state, CancellationToken ct)
    {
        state.LicenseKey = await ResolveSecretAsync(state.LicenseKey, ct) ?? string.Empty;
        state.DbPassword = await ResolveSecretAsync(state.DbPassword, ct) ?? string.Empty;
        state.RedisPassword = await ResolveSecretAsync(state.RedisPassword, ct);
        state.MinioSecretKey = await ResolveSecretAsync(state.MinioSecretKey, ct);
        state.AzureClientSecret = await ResolveSecretAsync(state.AzureClientSecret, ct);
        state.SmtpPassword = await ResolveSecretAsync(state.SmtpPassword, ct) ?? string.Empty;
        state.CloudflareTunnelToken = await ResolveSecretAsync(state.CloudflareTunnelToken, ct);
        state.AdminPassword = await ResolveSecretAsync(state.AdminPassword, ct) ?? string.Empty;
    }

    private async Task<string?> StoreSecretAsync(string tokenHash, string fieldName, string? value, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.StartsWith(SecretReferencePrefix, StringComparison.Ordinal)) return value;
        var secretName = $"/wizard/{tokenHash.ToLowerInvariant()}/{fieldName}";
        await _secrets.SetSecretAsync(secretName, value, ct);
        return SecretReferencePrefix + secretName;
    }

    private async Task<string?> ResolveSecretAsync(string? value, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(SecretReferencePrefix, StringComparison.Ordinal)) return value;
        return await _secrets.GetSecretAsync(value[SecretReferencePrefix.Length..], ct);
    }

    private static WizardStateDto Clone(WizardStateDto state)
        => JsonSerializer.Deserialize<WizardStateDto>(JsonSerializer.Serialize(state, JsonOptions), JsonOptions)
            ?? throw new InvalidOperationException("The setup state could not be serialized.");
}
