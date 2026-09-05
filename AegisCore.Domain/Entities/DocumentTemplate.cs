using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("DocumentTemplates")]
public sealed class DocumentTemplate : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, Column(TypeName = "text")]
    public string RawHtml { get; set; } = string.Empty;
    [Required, Column(TypeName = "jsonb")]
    public string ExtractedPalette { get; set; } = "[]";
    [Required, Column(TypeName = "jsonb")]
    public string ExtractedFonts { get; set; } = "[]";
    [MaxLength(2048)]
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
