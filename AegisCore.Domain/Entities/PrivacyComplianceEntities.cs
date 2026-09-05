using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("FerpaAuditLogs")]
public sealed class FerpaAuditLog : ITenantScoped
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    [Required, MaxLength(300)] public string UserRole { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public Guid StudentId { get; set; }
    [Required, MaxLength(20)] public string Action { get; set; } = "Read";
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    [NotMapped] public string? IpAddress { get; private set; }
    [Column("IpAddress")] public byte[]? IpAddressEncrypted { get; private set; }
    public bool Justified { get; set; }
    public bool UserErased { get; set; }
    public bool StudentErased { get; set; }

    public void SetEncryptedIpAddress(string? value, byte[]? ciphertext)
    {
        IpAddress = value;
        IpAddressEncrypted = ciphertext;
    }

    public void EraseIdentifiers(Guid erasedUserId)
    {
        if (UserId == erasedUserId) { UserId = Guid.Empty; UserErased = true; }
        if (StudentId == erasedUserId) { StudentId = Guid.Empty; StudentErased = true; }
    }
}

[Table("ConsentLedgers")]
public sealed class ConsentLedger : ITenantScoped
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    [Required, MaxLength(80)] public string DataCategory { get; set; } = string.Empty;
    public DateTime ConsentedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    [Required, MaxLength(8000)] public string ConsentText { get; set; } = string.Empty;
    [NotMapped] public string? IpAddress { get; private set; }
    [Column("IpAddress")] public byte[]? IpAddressEncrypted { get; private set; }

    public void SetEncryptedIpAddress(string? value, byte[]? ciphertext)
    {
        IpAddress = value;
        IpAddressEncrypted = ciphertext;
    }

    public void Revoke(DateTime at) => RevokedAt ??= at;
}

[Table("ErasureRequests")]
public sealed class ErasureRequest : ITenantScoped
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime DeadlineAt { get; set; }
    public DateTime? ErasedAt { get; set; }
    public ErasureRequestStatus Status { get; set; } = ErasureRequestStatus.Pending;
    [MaxLength(4000)] public string? FailureReason { get; set; }
}
