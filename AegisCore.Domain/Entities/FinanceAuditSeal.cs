using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceAuditSeals")]
public sealed class FinanceAuditSeal : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int EventCount { get; set; }
    [Required, MaxLength(80)] public string Algorithm { get; set; } = "ML-DSA-65";
    [Required, MaxLength(200)] public string KeyId { get; set; } = string.Empty;
    [Required] public byte[] MerkleRoot { get; set; } = [];
    [Required] public byte[] Signature { get; set; } = [];
    [Required] public byte[] PublicKey { get; set; } = [];
    public DateTime SealedAt { get; set; } = DateTime.UtcNow;
}
