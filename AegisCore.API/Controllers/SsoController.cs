using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Identity;
using AegisCore.API.Authorization;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Identity;
using AegisCore.Infrastructure.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace AegisCore.API.Controllers;

/// <summary>
/// Sign-in through the identity provider(s) a university chose in the setup wizard — Microsoft
/// Entra ID, Google Workspace or Okta — over one generic OpenID Connect handler. The provider
/// authenticates the person; this API then issues the same first-party token a password login
/// issues, so nothing downstream knows or cares which door was used.
///
/// Lives under /api/auth so the licence lock treats it like a login (people must still be able to
/// sign in to read their records); the one write a first sign-in makes — creating the person — is
/// refused with the licence sentence explicitly.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/auth/sso")]
public sealed class SsoController(
    AegisDbContext db,
    IJwtTokenService tokens,
    IConfiguration configuration,
    IConnectionMultiplexer redis,
    IExternalSignInService signIn,
    ISignInHandoffService handoff,
    ILogger<SsoController> logger,
    ICasbinPolicySyncService? casbin = null) : ControllerBase
{
    /// <summary>What the login screen should offer this tenant.</summary>
    [HttpGet("providers")]
    public async Task<IActionResult> Providers([FromQuery] Guid? tenant, CancellationToken ct)
    {
        var tenantId = ResolveTenantId(tenant);
        if (tenantId == Guid.Empty) return Ok(new { localEnabled = true, providers = Array.Empty<object>() });
        var methods = await signIn.GetLoginMethodsAsync(tenantId, ct);
        return Ok(new
        {
            localEnabled = methods.LocalEnabled,
            providers = methods.Providers.Select(p => new { type = p.Type, displayName = p.DisplayName, startPath = p.StartPath })
        });
    }

    /// <summary>Begins the sign-in: PKCE and state go to Redis, the browser goes to the provider.</summary>
    [HttpGet("{provider}/start")]
    public async Task<IActionResult> Start(string provider, [FromQuery] Guid? tenant, [FromQuery] string? returnTo, CancellationToken ct)
    {
        if (!ExternalSignInService.TryParseType(provider, out var type) || type == AegisCore.Domain.IdentityProviderType.Local)
            return LoginWithError("provider_unknown", "That sign-in method does not exist.");
        var tenantId = ResolveTenantId(tenant);
        if (tenantId == Guid.Empty) return LoginWithError("tenant_unknown", "This installation has no tenant configured; the setup wizard has not been completed.");
        try
        {
            var start = await signIn.StartAsync(tenantId, type, returnTo, ct);
            return Redirect(start.AuthorizationUrl);
        }
        catch (ExternalSignInException ex)
        {
            logger.LogWarning("SSO start refused for {Provider}: {Code} {Message}", provider, ex.Code, ex.Message);
            return LoginWithError(ex.Code, ex.Message);
        }
    }

    /// <summary>
    /// The provider's redirect. Validates state and the ID token, matches or creates the person,
    /// then hands the portal a one-time code: the portal keeps the session in cookies it sets
    /// itself, so the tokens never travel in a URL.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, [FromQuery(Name = "error_description")] string? errorDescription, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            logger.LogWarning("SSO provider answered with error {Error}: {Description}", error, errorDescription);
            return LoginWithError("provider_refused", error == "access_denied"
                ? "The identity provider did not let you sign in to this application. Ask your administrator to grant you access."
                : $"The identity provider refused the sign-in ({error}). {errorDescription}".Trim());
        }
        try
        {
            var result = await signIn.CompleteAsync(state ?? string.Empty, code ?? string.Empty, ct);
            var handoffCode = await handoff.IssueAsync(result, ct);
            return Redirect($"{PortalUrl()}/login/sso?code={Uri.EscapeDataString(handoffCode)}");
        }
        catch (ExternalSignInException ex)
        {
            logger.LogWarning("SSO callback refused: {Code} {Message}", ex.Code, ex.Message);
            return LoginWithError(ex.Code, ex.Message);
        }
        catch (LicenseReadOnlyException ex)
        {
            // The one message a brand-new person must be able to act on, verbatim from the licence code.
            logger.LogWarning("SSO first sign-in refused by licence lock: {Reason}", ex.Reason);
            return LoginWithError("license_read_only", ex.Message);
        }
    }

    /// <summary>The portal trades the one-time code for the same tokens a password login returns.</summary>
    [HttpPost("exchange")]
    public async Task<IActionResult> Exchange([FromBody] ExchangeRequest request, CancellationToken ct)
    {
        var result = await handoff.RedeemAsync(request.Code ?? string.Empty, ct);
        if (result is null) return Unauthorized(new { error = "This sign-in code has expired or was already used. Sign in again." });
        var issuer = new SessionTokenIssuer(db, tokens, redis, configuration, casbin);
        var user = await issuer.LoadForSignInAsync(result.TenantId, result.UserId, ct);
        if (user is null || user.Status != AegisCore.Domain.UserStatus.Active)
            return Unauthorized(new { error = "Your account here is not active. Ask your registrar's office." });
        var issued = await issuer.IssueAsync(user, ct);
        return Ok(new { accessToken = issued.AccessToken, refreshToken = issued.RefreshToken, tokenType = issued.TokenType, expiresIn = issued.ExpiresIn });
    }

    /// <summary>
    /// The tenant a sign-in is for. One container per university carries it in TENANT_ID; a shared
    /// deployment names it in the query (the login page passes it along), and the stored provider
    /// configuration is per tenant either way.
    /// </summary>
    private Guid ResolveTenantId(Guid? requested)
    {
        if (Guid.TryParse(configuration["TENANT_ID"], out var configured) && configured != Guid.Empty) return configured;
        return requested is { } explicitTenant && explicitTenant != Guid.Empty ? explicitTenant : Guid.Empty;
    }

    private string PortalUrl() => (configuration["APP_URL"] ?? "http://localhost:3000").TrimEnd('/');

    private RedirectResult LoginWithError(string code, string message)
        => Redirect($"{PortalUrl()}/login?sso_error={Uri.EscapeDataString(code)}&message={Uri.EscapeDataString(message)}");

    public sealed record ExchangeRequest(string? Code);
}
