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

    [MaxLength(2000)]
    public string? PasswordHash { get; set; }

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;

    [MaxLength(500)]
    public string? ExternalId { get; set; }

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
