using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceRegulatorySchemas")]
public sealed class FinanceRegulatorySchema : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(30)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Jurisdiction { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Version { get; set; } = string.Empty;
    [Required, MaxLength(10)] public string Format { get; set; } = "XML";
    [Required, MaxLength(100)] public string RootElement { get; set; } = string.Empty;
    [MaxLength(500)] public string? NamespaceUri { get; set; }
    [Required, Column(TypeName = "jsonb")] public string DefinitionJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("FinanceRegulatorySubmissions")]
public sealed class FinanceRegulatorySubmission : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid RegulatorySchemaId { get; set; }
    [Required, MaxLength(100)] public string PeriodLabel { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string StorageKey { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string ContentHash { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string PqcAlgorithm { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string PqcKeyId { get; set; } = string.Empty;
    [Required] public byte[] PqcSignature { get; set; } = [];
    [Required] public byte[] PqcPublicKey { get; set; } = [];
    public Guid GeneratedByUserId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
