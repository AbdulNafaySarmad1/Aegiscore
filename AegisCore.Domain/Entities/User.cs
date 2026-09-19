using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Users")]
public class User : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Personal recovery address. After admission migration the institutional email becomes
    /// the primary login and the applicant's personal email is retained here for password reset only.
    /// </summary>
    [MaxLength(320)]
    public string? RecoveryEmail { get; set; }

    [MaxLength(2000)]
    public string? PasswordHash { get; set; }

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;

    /// <summary>
    /// The identity provider's subject for this person (Entra "oid", Google "sub", Okta "sub").
    /// Meaningful only together with <see cref="ExternalIssuer"/> and the tenant: two directories
    /// can hand out the same subject value for two different people.
    /// </summary>
    [MaxLength(500)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// The issuer that vouched for <see cref="ExternalId"/>, as it appears in the ID token's "iss"
    /// claim (for Entra that includes the directory id). An external identity is looked up by
    /// (tenant, issuer, subject), never by subject alone.
    /// </summary>
    [MaxLength(500)]
    public string? ExternalIssuer { get; set; }

    [MaxLength(1000)]
    public string? ExternalClaimsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    [MaxLength(500)]
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    public Guid TenantId { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
