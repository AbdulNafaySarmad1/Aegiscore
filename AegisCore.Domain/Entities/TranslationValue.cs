using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("TranslationValues")]
public class TranslationValue : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; set; }

    public Guid TranslationKeyId { get; set; }

    [Required]
    [MaxLength(20)]
    public string LanguageCode { get; set; } = "en";

    [Required]
    [MaxLength(2000)]
    public string Value { get; set; } = string.Empty;

    public bool IsMachineTranslated { get; set; }

    public DateTime? LastReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TranslationKeyId))]
    public TranslationKey TranslationKey { get; set; } = null!;
}
