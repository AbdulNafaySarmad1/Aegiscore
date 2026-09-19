using AegisCore.Domain;

namespace AegisCore.Application.Identity;

/// <summary>
/// What an identity provider told us about the person who just signed in, after the ID token was
/// validated. <see cref="Issuer"/> and <see cref="Subject"/> together are the person's identity at
/// that provider; the email is what they are called here and may change.
/// </summary>
public sealed record ExternalIdentity(
    string Issuer,
    string Subject,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyDictionary<string, string> Claims,
    IReadOnlyList<string> Groups);

/// <summary>A login method the login screen offers for one tenant.</summary>
public sealed record LoginMethod(string Type, string DisplayName, string? StartPath);

/// <summary>What the login screen shows: which buttons, and whether the password form is one of them.</summary>
public sealed record LoginMethods(bool LocalEnabled, IReadOnlyList<LoginMethod> Providers);

/// <summary>Where to send the browser to begin a sign-in at the provider.</summary>
public sealed record ExternalSignInStart(string AuthorizationUrl);

/// <summary>The person the provider vouched for, matched or created here, with roles applied.</summary>
public sealed record ExternalSignInResult(Guid UserId, Guid TenantId, IdentityProviderType Provider, string Issuer, IReadOnlyDictionary<string, string> Claims);

/// <summary>
/// The provider said no, or what it said could not be trusted. <see cref="Code"/> is stable and
/// short for the login screen to branch on; <see cref="Message"/> is the sentence it shows.
/// </summary>
public sealed class ExternalSignInException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>
/// One generic OpenID Connect (authorization code + PKCE) sign-in, parameterised by the tenant's
/// stored provider configuration. Entra, Google and Okta differ only in configuration.
/// </summary>
public interface IExternalSignInService
{
    /// <summary>The login methods enabled for a tenant. No configuration at all means Local only.</summary>
    Task<LoginMethods> GetLoginMethodsAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Is password sign-in allowed for this tenant? False only when SSO is configured and Local is not.</summary>
    Task<bool> IsLocalSignInEnabledAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Builds the authorization URL and remembers state, PKCE verifier and nonce for the callback.</summary>
    Task<ExternalSignInStart> StartAsync(Guid tenantId, IdentityProviderType provider, string? returnTo, CancellationToken ct = default);

    /// <summary>
    /// Handles the provider's redirect: checks state, exchanges the code, validates the ID token,
    /// matches or creates the person and applies role mapping. Throws <see cref="ExternalSignInException"/>
    /// for anything the provider or its configuration got wrong.
    /// </summary>
    Task<ExternalSignInResult> CompleteAsync(string state, string code, CancellationToken ct = default);
}

/// <summary>
/// Hands a finished sign-in to the portal. The provider's redirect lands on the API, but the portal
/// keeps its session in cookies it sets itself, so the API answers with a short-lived one-time code
/// the portal exchanges for tokens — the tokens never travel in a URL.
/// </summary>
public interface ISignInHandoffService
{
    Task<string> IssueAsync(ExternalSignInResult result, CancellationToken ct = default);
    /// <summary>Consumes the code; null when it is unknown, expired, or already used.</summary>
    Task<ExternalSignInResult?> RedeemAsync(string code, CancellationToken ct = default);
}
