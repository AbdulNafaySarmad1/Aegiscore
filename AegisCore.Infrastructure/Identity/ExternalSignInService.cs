using System.Text.Json;
using AegisCore.Application.Identity;
using AegisCore.Application.Licensing;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Identity.Oidc;
using AegisCore.Infrastructure.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Identity;

/// <summary>
/// Sign-in through a tenant's configured identity provider, end to end: which methods the login
/// screen offers, the redirect to the provider, and the callback that turns a validated ID token
/// into a person here with the right roles. Token issuance is not done here — the API issues the
/// same first-party token it issues after a password login, from the user this returns.
/// </summary>
public sealed class ExternalSignInService(
    AegisDbContext db,
    ISecretService secrets,
    IOidcClient oidc,
    IConnectionMultiplexer redis,
    IIdentityService identity,
    IUserRepository users,
    IConfiguration configuration,
    ILogger<ExternalSignInService> logger,
    ILicenseService? license = null) : IExternalSignInService
{
    private const string StateKeyPrefix = "aegiscore:auth:sso:state:";
    private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>The one redirect URI this installation registers at every provider.</summary>
    public static string RedirectUri(IConfiguration configuration)
        => (configuration["APP_URL"] ?? "http://localhost:3000").TrimEnd('/') + "/api/auth/sso/callback";

    public static string SecretName(Guid tenantId, IdentityProviderType type)
        => $"/tenants/{tenantId:D}/identity/{type.ToString().ToLowerInvariant()}/client-secret";

    public async Task<LoginMethods> GetLoginMethodsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var rows = await EnabledAsync(tenantId, ct);
        // An installation set up before providers existed has no rows: it keeps working as it did.
        var localEnabled = rows.Count == 0 || rows.Any(r => r.ProviderType == IdentityProviderType.Local);
        var providers = rows.Where(r => r.ProviderType != IdentityProviderType.Local)
            .Select(r => new LoginMethod(Slug(r.ProviderType), r.DisplayName, $"/api/auth/sso/{Slug(r.ProviderType)}/start"))
            .ToList();
        return new LoginMethods(localEnabled, providers);
    }

    public async Task<bool> IsLocalSignInEnabledAsync(Guid tenantId, CancellationToken ct = default)
        => (await GetLoginMethodsAsync(tenantId, ct)).LocalEnabled;

    public async Task<ExternalSignInStart> StartAsync(Guid tenantId, IdentityProviderType type, string? returnTo, CancellationToken ct = default)
    {
        var row = await ProviderRowAsync(tenantId, type, ct);
        var provider = await ResolveAsync(row, ct);
        var state = OidcClient.RandomToken();
        var nonce = OidcClient.RandomToken();
        var verifier = OidcClient.RandomToken(48);
        var pending = new PendingSignIn(tenantId, row.Id, nonce, verifier, SafeReturnTo(returnTo));
        await redis.GetDatabase().StringSetAsync(StateKeyPrefix + state, JsonSerializer.Serialize(pending, Json), StateTtl);
        var url = await oidc.BuildAuthorizationUrlAsync(provider, state, nonce, OidcClient.CodeChallenge(verifier), ct);
        return new ExternalSignInStart(url);
    }

    public async Task<ExternalSignInResult> CompleteAsync(string state, string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(code))
            throw new ExternalSignInException("callback_incomplete", "The identity provider sent the browser back without a code. Start the sign-in again.");
        var stored = await redis.GetDatabase().StringGetDeleteAsync(StateKeyPrefix + state);
        if (!stored.HasValue)
            throw new ExternalSignInException("state_invalid", "This sign-in attempt has expired or was already used. Start again from the login page.");
        var pending = JsonSerializer.Deserialize<PendingSignIn>(stored.ToString(), Json)
            ?? throw new ExternalSignInException("state_invalid", "This sign-in attempt could not be read. Start again from the login page.");

        var row = await db.IdentityProviderConfigurations.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == pending.ProviderId && r.TenantId == pending.TenantId && r.IsEnabled, ct)
            ?? throw new ExternalSignInException("provider_not_configured", "This way of signing in is no longer enabled for this university.");
        var provider = await ResolveAsync(row, ct);

        var tokens = await oidc.ExchangeCodeAsync(provider, code, pending.CodeVerifier, ct);
        var external = await oidc.ValidateAsync(provider, tokens, pending.Nonce, ct);

        await RefuseFirstSignInWhenLicenceLockedAsync(pending.TenantId, external, ct);

        var user = await identity.UpsertUserFromExternalIdentityAsync(external, pending.TenantId, AuthProviderFor(row.ProviderType), ct);
        if (user.Status != UserStatus.Active)
            throw new ExternalSignInException("account_inactive", "Your account here is not active. Ask your registrar's office.");
        await ApplyRoleMappingAsync(user, row, external.Groups, ct);

        logger.LogInformation("SSO sign-in completed via {Provider} for user {UserId} (issuer {Issuer})", row.ProviderType, user.Id, external.Issuer);
        return new ExternalSignInResult(user.Id, pending.TenantId, row.ProviderType, external.Issuer, external.Claims);
    }

    /// <summary>
    /// A first sign-in creates a person, which is a write, and the licence write interceptor exempts
    /// /api/auth so people can keep signing in to read their records. The brand-new person gets the
    /// same sentence everyone else sees when the lock bites, instead of a silent half-created account.
    /// A person who already exists signs in as a password login would.
    /// </summary>
    private async Task RefuseFirstSignInWhenLicenceLockedAsync(Guid tenantId, ExternalIdentity external, CancellationToken ct)
    {
        if (license is null || configuration.GetValue<bool>("Runtime:DemoMode")) return;
        var status = await license.GetStatusAsync(ct);
        if (!status.IsLocked) return;
        var known = await users.GetByExternalIdentityAsync(tenantId, external.Issuer, external.Subject, ct)
            ?? (string.IsNullOrWhiteSpace(external.Email) ? null : await users.GetByTenantAndEmailAsync(tenantId, external.Email.Trim().ToLowerInvariant(), ct));
        if (known is not null) return;
        throw new LicenseReadOnlyException(LicenseLockDescription.Why(status) + ", so no new account can be created for " + external.Email);
    }

    /// <summary>
    /// Provider groups → AegisCore roles, per the tenant's mapping. Roles the mapping controls (its
    /// targets and the default) are kept in step with the person's groups on every sign-in; roles an
    /// administrator gave by hand, outside the mapping, are left alone.
    /// </summary>
    private async Task ApplyRoleMappingAsync(User user, IdentityProviderConfiguration row, IReadOnlyList<string> groups, CancellationToken ct)
    {
        var mapping = ParseMapping(row.GroupRoleMappingJson);
        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
            if (mapping.TryGetValue(group, out var role) && !string.IsNullOrWhiteSpace(role)) wanted.Add(role.Trim());
        if (wanted.Count == 0 && !string.IsNullOrWhiteSpace(row.DefaultRole)) wanted.Add(row.DefaultRole.Trim());

        var managed = new HashSet<string>(mapping.Values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()), StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(row.DefaultRole)) managed.Add(row.DefaultRole.Trim());
        if (managed.Count == 0) return;

        var tenantRoles = await db.Roles.IgnoreQueryFilters().Where(r => r.TenantId == user.TenantId).ToListAsync(ct);
        var byName = tenantRoles.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var current = await db.UserRoles.IgnoreQueryFilters().Include(ur => ur.Role)
            .Where(ur => ur.UserId == user.Id && ur.TenantId == user.TenantId).ToListAsync(ct);

        var changed = false;
        foreach (var name in wanted)
        {
            if (current.Any(ur => string.Equals(ur.Role?.Name, name, StringComparison.OrdinalIgnoreCase))) continue;
            if (!byName.TryGetValue(name, out var role))
            {
                logger.LogWarning("SSO role mapping names role '{Role}' which does not exist in tenant {TenantId}", name, user.TenantId);
                continue;
            }
            db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), TenantId = user.TenantId, UserId = user.Id, RoleId = role.Id, AssignedByUserId = user.Id, AssignedAt = DateTime.UtcNow });
            changed = true;
        }
        foreach (var assignment in current)
        {
            var name = assignment.Role?.Name;
            if (name is null || !managed.Contains(name) || wanted.Contains(name)) continue;
            db.UserRoles.Remove(assignment);
            changed = true;
        }
        if (changed) await db.SaveChangesAsync(ct);
    }

    private static Dictionary<string, string> ParseMapping(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new(StringComparer.OrdinalIgnoreCase);
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            return new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException) { return new(StringComparer.OrdinalIgnoreCase); }
    }

    private async Task<List<IdentityProviderConfiguration>> EnabledAsync(Guid tenantId, CancellationToken ct)
        => await db.IdentityProviderConfigurations.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.IsEnabled)
            .OrderBy(r => r.SortOrder).ThenBy(r => r.ProviderType)
            .ToListAsync(ct);

    private async Task<IdentityProviderConfiguration> ProviderRowAsync(Guid tenantId, IdentityProviderType type, CancellationToken ct)
    {
        if (type == IdentityProviderType.Local)
            throw new ExternalSignInException("provider_not_configured", "Local sign-in uses the email and password form, not a redirect.");
        return await db.IdentityProviderConfigurations.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(r => r.TenantId == tenantId && r.ProviderType == type && r.IsEnabled, ct)
            ?? throw new ExternalSignInException("provider_not_configured", $"{Display(type)} sign-in is not enabled for this university.");
    }

    private async Task<OidcProvider> ResolveAsync(IdentityProviderConfiguration row, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(row.Authority) || string.IsNullOrWhiteSpace(row.ClientId) || string.IsNullOrWhiteSpace(row.ClientSecretName))
            throw new ExternalSignInException("provider_incomplete", $"{row.DisplayName} is not fully configured (authority, client id and client secret are all required).");
        string secret;
        try { secret = await secrets.GetSecretAsync(row.ClientSecretName, ct); }
        catch (KeyNotFoundException)
        {
            logger.LogError("Client secret {SecretName} for provider {Provider} is missing from the vault", row.ClientSecretName, row.ProviderType);
            throw new ExternalSignInException("provider_incomplete", $"The client secret for {row.DisplayName} is missing from the installation's secret vault. An administrator must enter it again.");
        }
        // The redirect URI follows the installation's current APP_URL — the one the wizard showed and
        // the login screen lives at; the row's copy records what was registered at the provider.
        return new OidcProvider(row.ProviderType, row.Authority, row.ClientId, secret,
            string.IsNullOrWhiteSpace(configuration["APP_URL"]) && !string.IsNullOrWhiteSpace(row.RedirectUri) ? row.RedirectUri : RedirectUri(configuration),
            string.IsNullOrWhiteSpace(row.Scopes) ? "openid profile email" : row.Scopes,
            row.ExternalTenantId, row.AllowedDomain, string.IsNullOrWhiteSpace(row.GroupClaimName) ? "groups" : row.GroupClaimName);
    }

    /// <summary>Only a path inside the portal is remembered; an absolute URL would be an open redirect.</summary>
    private static string? SafeReturnTo(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal) && value.Length <= 512 ? value : null;

    public static string Slug(IdentityProviderType type) => type.ToString().ToLowerInvariant();

    public static string Display(IdentityProviderType type) => type switch
    {
        IdentityProviderType.Entra => "Microsoft Entra ID",
        IdentityProviderType.Google => "Google Workspace",
        IdentityProviderType.Okta => "Okta",
        _ => "Email and password"
    };

    public static bool TryParseType(string? slug, out IdentityProviderType type)
    {
        switch (slug?.Trim().ToLowerInvariant())
        {
            case "entra": case "microsoft": case "azuread": type = IdentityProviderType.Entra; return true;
            case "google": type = IdentityProviderType.Google; return true;
            case "okta": type = IdentityProviderType.Okta; return true;
            case "local": type = IdentityProviderType.Local; return true;
            default: type = default; return false;
        }
    }

    public static AuthProvider AuthProviderFor(IdentityProviderType type) => type switch
    {
        IdentityProviderType.Entra => AuthProvider.AzureAd,
        IdentityProviderType.Google => AuthProvider.Google,
        IdentityProviderType.Okta => AuthProvider.Oidc,
        _ => AuthProvider.Local
    };

    private sealed record PendingSignIn(Guid TenantId, Guid ProviderId, string Nonce, string CodeVerifier, string? ReturnTo);
}

/// <summary>
/// One-time codes in Redis: 60 seconds, consumed on first use. The provider's redirect lands on the
/// API; the portal sets its own HttpOnly cookies, so it exchanges the code for tokens itself.
/// </summary>
public sealed class SignInHandoffService(IConnectionMultiplexer redis) : ISignInHandoffService
{
    private const string KeyPrefix = "aegiscore:auth:sso:handoff:";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<string> IssueAsync(ExternalSignInResult result, CancellationToken ct = default)
    {
        var code = OidcClient.RandomToken();
        var stored = new Stored(result.UserId, result.TenantId, result.Provider, result.Issuer);
        await redis.GetDatabase().StringSetAsync(KeyPrefix + code, JsonSerializer.Serialize(stored, Json), Ttl);
        return code;
    }

    public async Task<ExternalSignInResult?> RedeemAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 128) return null;
        var value = await redis.GetDatabase().StringGetDeleteAsync(KeyPrefix + code);
        if (!value.HasValue) return null;
        var stored = JsonSerializer.Deserialize<Stored>(value.ToString(), Json);
        return stored is null ? null : new ExternalSignInResult(stored.UserId, stored.TenantId, stored.Provider, stored.Issuer, new Dictionary<string, string>());
    }

    private sealed record Stored(Guid UserId, Guid TenantId, IdentityProviderType Provider, string Issuer);
}
