using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Application.Identity;
using AegisCore.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AegisCore.Infrastructure.Identity.Oidc;

/// <summary>Everything the OIDC round-trip needs about one provider, resolved from the tenant's row plus the vault.</summary>
public sealed record OidcProvider(
    IdentityProviderType Type,
    string Authority,
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    string Scopes,
    string? ExternalTenantId,
    string? AllowedDomain,
    string GroupClaimName);

public sealed record OidcTokens(string IdToken, string? AccessToken);

/// <summary>
/// The provider-neutral half of OpenID Connect: discovery, the authorization URL, the code exchange
/// (confidential client — the secret never leaves the API) and ID-token validation against the
/// provider's published keys. What differs between Entra, Google and Okta is data, not code.
/// </summary>
public interface IOidcClient
{
    Task<OpenIdConnectConfiguration> DiscoverAsync(string authority, CancellationToken ct = default);
    Task<string> BuildAuthorizationUrlAsync(OidcProvider provider, string state, string nonce, string codeChallenge, CancellationToken ct = default);
    Task<OidcTokens> ExchangeCodeAsync(OidcProvider provider, string code, string codeVerifier, CancellationToken ct = default);
    Task<ExternalIdentity> ValidateAsync(OidcProvider provider, OidcTokens tokens, string expectedNonce, CancellationToken ct = default);
}

public sealed class OidcClient(IHttpClientFactory clients, ILogger<OidcClient> logger) : IOidcClient
{
    public const string HttpClientName = "oidc";
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> Discovery = new(StringComparer.OrdinalIgnoreCase);

    public static string DiscoveryUrl(string authority) => authority.TrimEnd('/') + "/.well-known/openid-configuration";

    public async Task<OpenIdConnectConfiguration> DiscoverAsync(string authority, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ExternalSignInException("authority_invalid", "The identity provider's authority must be an HTTPS URL.");
        var manager = Discovery.GetOrAdd(authority.TrimEnd('/'), key => new ConfigurationManager<OpenIdConnectConfiguration>(
            DiscoveryUrl(key), new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(clients.CreateClient(HttpClientName)) { RequireHttps = true }));
        try
        {
            return await manager.GetConfigurationAsync(ct);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException or IOException)
        {
            logger.LogWarning(ex, "OIDC discovery failed for {Authority}", authority);
            throw new ExternalSignInException("provider_unreachable",
                "The identity provider could not be reached to read its configuration. Check outbound HTTPS access and the authority URL, then try again.");
        }
    }

    public async Task<string> BuildAuthorizationUrlAsync(OidcProvider provider, string state, string nonce, string codeChallenge, CancellationToken ct = default)
    {
        var discovery = await DiscoverAsync(provider.Authority, ct);
        if (string.IsNullOrWhiteSpace(discovery.AuthorizationEndpoint))
            throw new ExternalSignInException("provider_incomplete", "The identity provider's configuration has no authorization endpoint.");
        var query = new Dictionary<string, string>
        {
            ["client_id"] = provider.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = provider.RedirectUri,
            ["scope"] = provider.Scopes,
            ["state"] = state,
            ["nonce"] = nonce,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["response_mode"] = "query"
        };
        // Google only takes a hosted-domain hint here; the "hd" claim on the ID token is what is enforced.
        if (provider.Type == IdentityProviderType.Google && !string.IsNullOrWhiteSpace(provider.AllowedDomain)) query["hd"] = provider.AllowedDomain;
        var separator = discovery.AuthorizationEndpoint.Contains('?') ? '&' : '?';
        return discovery.AuthorizationEndpoint + separator + string.Join('&', query.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));
    }

    public async Task<OidcTokens> ExchangeCodeAsync(OidcProvider provider, string code, string codeVerifier, CancellationToken ct = default)
    {
        var discovery = await DiscoverAsync(provider.Authority, ct);
        if (string.IsNullOrWhiteSpace(discovery.TokenEndpoint))
            throw new ExternalSignInException("provider_incomplete", "The identity provider's configuration has no token endpoint.");
        using var request = new HttpRequestMessage(HttpMethod.Post, discovery.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = provider.RedirectUri,
                ["client_id"] = provider.ClientId,
                ["client_secret"] = provider.ClientSecret,
                ["code_verifier"] = codeVerifier
            })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        string body;
        System.Net.HttpStatusCode status;
        try
        {
            using var response = await clients.CreateClient(HttpClientName).SendAsync(request, ct);
            status = response.StatusCode;
            body = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "OIDC token exchange failed to reach {Endpoint}", discovery.TokenEndpoint);
            throw new ExternalSignInException("provider_unreachable", "The identity provider could not be reached to complete the sign-in. Try again in a moment.");
        }
        JsonDocument json;
        try { json = JsonDocument.Parse(body); }
        catch (JsonException)
        {
            throw new ExternalSignInException("token_exchange_failed", $"The identity provider answered the sign-in with HTTP {(int)status} and no readable response.");
        }
        using (json)
        {
            var root = json.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                var description = root.TryGetProperty("error_description", out var d) ? d.GetString() : null;
                logger.LogWarning("OIDC token exchange refused: {Error} {Description}", error.GetString(), description);
                throw new ExternalSignInException("token_exchange_failed",
                    $"The identity provider refused to complete the sign-in ({error.GetString()}). {Hint(error.GetString())}");
            }
            if (!root.TryGetProperty("id_token", out var idToken) || idToken.ValueKind != JsonValueKind.String)
                throw new ExternalSignInException("id_token_missing", "The identity provider did not return an ID token. Make sure the application is registered for OpenID Connect and the 'openid' scope is granted.");
            var accessToken = root.TryGetProperty("access_token", out var at) && at.ValueKind == JsonValueKind.String ? at.GetString() : null;
            return new OidcTokens(idToken.GetString()!, accessToken);
        }
    }

    private static string Hint(string? error) => error switch
    {
        "invalid_client" => "The client secret is wrong or has expired; enter a current one in the identity provider settings.",
        "invalid_grant" => "The authorization code was already used or has expired; start the sign-in again.",
        "redirect_uri_mismatch" => "The redirect URI registered at the provider does not match this installation's.",
        _ => "Check the client id, client secret and redirect URI registered at the provider."
    };

    public async Task<ExternalIdentity> ValidateAsync(OidcProvider provider, OidcTokens tokens, string expectedNonce, CancellationToken ct = default)
    {
        var discovery = await DiscoverAsync(provider.Authority, ct);
        var handler = new JsonWebTokenHandler();
        TokenValidationParameters Parameters(ICollection<SecurityKey> keys) => new()
        {
            ValidateIssuer = true,
            ValidIssuer = discovery.Issuer,
            ValidateAudience = true,
            ValidAudience = provider.ClientId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keys,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256]
        };

        var result = await handler.ValidateTokenAsync(tokens.IdToken, Parameters(discovery.SigningKeys));
        if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // The provider rotated its keys since we cached them: read them again, once.
            Discovery.TryGetValue(provider.Authority.TrimEnd('/'), out var manager);
            manager?.RequestRefresh();
            discovery = await DiscoverAsync(provider.Authority, ct);
            result = await handler.ValidateTokenAsync(tokens.IdToken, Parameters(discovery.SigningKeys));
        }
        if (!result.IsValid)
        {
            logger.LogWarning(result.Exception, "ID token from {Issuer} rejected", discovery.Issuer);
            throw new ExternalSignInException("id_token_invalid", result.Exception switch
            {
                SecurityTokenInvalidIssuerException => "The ID token was issued by a different directory than the one configured for this university.",
                SecurityTokenInvalidAudienceException => "The ID token was issued for a different application (client id mismatch).",
                SecurityTokenExpiredException => "The ID token has already expired; start the sign-in again.",
                _ => "The ID token could not be verified against the identity provider's published keys."
            });
        }

        var identity = result.ClaimsIdentity;
        string? First(params string[] names) => names.Select(n => identity.FindFirst(n)?.Value).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        if (!string.Equals(First("nonce"), expectedNonce, StringComparison.Ordinal))
            throw new ExternalSignInException("nonce_mismatch", "The sign-in response did not match the request that started it. Start the sign-in again.");

        // Entra: pin the directory when the administrator gave us its id. The issuer check above
        // already did this for a tenant-specific authority; "tid" is the belt to those braces.
        var tid = First("tid");
        if (provider.Type == IdentityProviderType.Entra && Guid.TryParse(provider.ExternalTenantId, out var expectedTenant)
            && (!Guid.TryParse(tid, out var actualTenant) || actualTenant != expectedTenant))
            throw new ExternalSignInException("wrong_directory", "This account belongs to a different Microsoft Entra directory than the one configured for this university.");

        // Google: only accounts in the Workspace domain. A consumer Gmail account carries no "hd".
        var hd = First("hd");
        if (provider.Type == IdentityProviderType.Google && !string.IsNullOrWhiteSpace(provider.AllowedDomain)
            && !string.Equals(hd, provider.AllowedDomain, StringComparison.OrdinalIgnoreCase))
            throw new ExternalSignInException("wrong_domain", $"Only Google Workspace accounts in {provider.AllowedDomain} can sign in here.");

        // Entra prefers "oid": it names the person across every application in the directory, where
        // "sub" is pairwise per application. Everyone else: "sub".
        var subject = provider.Type == IdentityProviderType.Entra ? First("oid", "sub") : First("sub");
        var email = First("email", "preferred_username", "upn") ?? string.Empty;
        if (provider.Type != IdentityProviderType.Google && !string.IsNullOrWhiteSpace(provider.AllowedDomain)
            && !email.EndsWith("@" + provider.AllowedDomain, StringComparison.OrdinalIgnoreCase))
            throw new ExternalSignInException("wrong_domain", $"Only accounts in {provider.AllowedDomain} can sign in here.");

        var (firstName, lastName) = Names(First("given_name"), First("family_name"), First("name"));
        var groups = new List<string>();
        foreach (var claim in identity.FindAll(provider.GroupClaimName)) groups.AddRange(Values(claim));
        // Entra application roles are the other common way a directory says "this person is an admin".
        foreach (var claim in identity.FindAll("roles")) groups.AddRange(Values(claim));

        var claimNames = First("_claim_names");
        if (provider.Type == IdentityProviderType.Entra && groups.Count == 0 && claimNames?.Contains("groups", StringComparison.Ordinal) == true)
            groups.AddRange(await EntraGroupsFromGraphAsync(tokens.AccessToken, ct));
        if (provider.Type == IdentityProviderType.Google && groups.Count == 0 && provider.Scopes.Contains("admin.directory.group", StringComparison.Ordinal))
            groups.AddRange(await GoogleGroupsFromDirectoryAsync(tokens.AccessToken, email, ct));

        var claims = identity.Claims
            .Where(c => c.Type is not ("nonce" or "at_hash" or "c_hash" or "aio" or "rh" or "uti"))
            .GroupBy(c => c.Type, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => string.Join(",", g.Select(c => c.Value)), StringComparer.Ordinal);

        return new ExternalIdentity(discovery.Issuer, subject ?? throw new ExternalSignInException("subject_missing", "The ID token names no subject."),
            email, firstName, lastName, claims, groups.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>A group claim may be one value per claim or one JSON array in a single claim; both come back as values.</summary>
    private static IEnumerable<string> Values(Claim claim)
    {
        var value = claim.Value.Trim();
        if (value.StartsWith('['))
        {
            try
            {
                using var array = JsonDocument.Parse(value);
                if (array.RootElement.ValueKind == JsonValueKind.Array)
                    return array.RootElement.EnumerateArray().Select(e => e.ToString()).Where(v => v.Length > 0).ToList();
            }
            catch (JsonException) { /* a literal group name that happens to start with '[' */ }
        }
        return [value];
    }

    private static (string First, string Last) Names(string? given, string? family, string? full)
    {
        if (!string.IsNullOrWhiteSpace(given) || !string.IsNullOrWhiteSpace(family)) return (given?.Trim() ?? string.Empty, family?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(full)) return (string.Empty, string.Empty);
        var parts = full.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], string.Empty);
    }

    /// <summary>
    /// A person in more than ~200 groups gets no "groups" claim, only a pointer ("_claim_names");
    /// Microsoft Graph answers with the real list. Needs the access token to carry a Graph scope
    /// (GroupMember.Read.All); without it Graph answers 403 and the person signs in with no groups.
    /// Both the id and the display name of every group are returned so a mapping may use either.
    /// </summary>
    private async Task<IReadOnlyList<string>> EntraGroupsFromGraphAsync(string? accessToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return [];
        var groups = new List<string>();
        var url = "https://graph.microsoft.com/v1.0/me/memberOf?$select=id,displayName&$top=999";
        try
        {
            var client = clients.CreateClient(HttpClientName);
            while (!string.IsNullOrWhiteSpace(url))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using var response = await client.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Microsoft Graph memberOf answered HTTP {Status}; groups overage could not be resolved", (int)response.StatusCode);
                    return groups;
                }
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                foreach (var item in json.RootElement.GetProperty("value").EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String) groups.Add(id.GetString()!);
                    if (item.TryGetProperty("displayName", out var name) && name.ValueKind == JsonValueKind.String) groups.Add(name.GetString()!);
                }
                url = json.RootElement.TryGetProperty("@odata.nextLink", out var next) ? next.GetString() : null;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            logger.LogWarning(ex, "Microsoft Graph memberOf failed; groups overage could not be resolved");
        }
        return groups;
    }

    /// <summary>
    /// Google puts no groups in an ID token. When the wizard requested the Directory scope
    /// (admin.directory.group.readonly), the Admin SDK lists the groups the person belongs to;
    /// it answers 403 unless the person may read groups, in which case they sign in with none.
    /// </summary>
    private async Task<IReadOnlyList<string>> GoogleGroupsFromDirectoryAsync(string? accessToken, string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(email)) return [];
        var groups = new List<string>();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://admin.googleapis.com/admin/directory/v1/groups?userKey=" + Uri.EscapeDataString(email));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await clients.CreateClient(HttpClientName).SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google Directory groups answered HTTP {Status}; no groups resolved for {Email}", (int)response.StatusCode, email);
                return groups;
            }
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (json.RootElement.TryGetProperty("groups", out var list))
                foreach (var item in list.EnumerateArray())
                {
                    if (item.TryGetProperty("email", out var groupEmail) && groupEmail.ValueKind == JsonValueKind.String) groups.Add(groupEmail.GetString()!);
                    if (item.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String) groups.Add(name.GetString()!);
                }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Google Directory groups failed for {Email}", email);
        }
        return groups;
    }

    // PKCE and state material: base64url without padding, as RFC 7636 wants.
    public static string RandomToken(int bytes = 32) => Base64Url(RandomNumberGenerator.GetBytes(bytes));
    public static string CodeChallenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    private static string Base64Url(byte[] data) => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
