using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

/// <summary>
/// One way of signing in to one tenant: Local (email and password), or an OpenID Connect provider
/// (Microsoft Entra ID, Google Workspace, Okta) with the credentials the wizard collected. A tenant
/// holds one row per enabled provider type; the login screen lists exactly these rows.
/// </summary>
[Table("IdentityProviderConfigurations")]
public class IdentityProviderConfiguration : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public IdentityProviderType ProviderType { get; set; }

    /// <summary>What the login button says: "Microsoft Entra ID", "Google Workspace", "Okta".</summary>
    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>OAuth client (application) id registered at the provider. Empty for Local.</summary>
    [MaxLength(300)]
    public string? ClientId { get; set; }

    /// <summary>
    /// Where the client secret lives: a name in the installation's secret vault, never the value.
    /// The vault is the same one the wizard and the payment gateways use, so the secret is
    /// encrypted at rest by whichever provider the installation chose (encrypted file, Infisical,
    /// a cloud key vault).
    /// </summary>
    [MaxLength(500)]
    public string? ClientSecretName { get; set; }

    /// <summary>
    /// The provider's issuer base, from which "/.well-known/openid-configuration" is fetched:
    /// Entra "https://login.microsoftonline.com/{tenant}/v2.0", Google "https://accounts.google.com",
    /// Okta "https://{org}.okta.com" or "https://{org}.okta.com/oauth2/default".
    /// </summary>
    [MaxLength(500)]
    public string? Authority { get; set; }

    /// <summary>Exactly what was registered at the provider as the redirect URI.</summary>
    [MaxLength(500)]
    public string? RedirectUri { get; set; }

    /// <summary>Space-separated scopes requested at authorization time.</summary>
    [MaxLength(500)]
    public string Scopes { get; set; } = "openid profile email";

    /// <summary>
    /// The provider's own tenant identifier: the Entra directory (tenant) id. Recorded so the
    /// accepted issuer can be pinned to that one directory rather than "common".
    /// </summary>
    [MaxLength(200)]
    public string? ExternalTenantId { get; set; }

    /// <summary>
    /// Google Workspace: the hosted domain an account must belong to (the "hd" claim). Other
    /// providers: an optional email-domain restriction applied after sign-in.
    /// </summary>
    [MaxLength(200)]
    public string? AllowedDomain { get; set; }

    /// <summary>Name of the claim carrying group memberships in the ID token ("groups" everywhere).</summary>
    [MaxLength(100)]
    public string GroupClaimName { get; set; } = "groups";

    /// <summary>
    /// JSON object mapping a provider group (object id or display name) to an AegisCore role name:
    /// {"3f2c...": "Admin", "Faculty Staff": "Faculty"}. Applied on every sign-in.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string GroupRoleMappingJson { get; set; } = "{}";

    /// <summary>
    /// Role given to a person whose groups match nothing in the mapping. Null means no role: the
    /// account exists but can open nothing until an administrator assigns one.
    /// </summary>
    [MaxLength(100)]
    public string? DefaultRole { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
