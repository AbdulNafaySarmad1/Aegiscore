using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("CountryPacks")]
public class CountryPack : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CountryCode { get; set; } = string.Empty;

    [MaxLength(20)]
    public string DefaultLanguage { get; set; } = "en";

    public TextDirection TextDirection { get; set; } = TextDirection.LTR;

    [MaxLength(20)]
    public string DefaultCurrency { get; set; } = "USD";

    [MaxLength(50)]
    public string? Timezone { get; set; }

    public CalendarType DefaultCalendarType { get; set; } = CalendarType.Semester;

    public GradingScheme DefaultGradingScheme { get; set; } = GradingScheme.GPA4;

    [MaxLength(20)]
    public string Version { get; set; } = "1.0.0";

    public CountryPackStatus Status { get; set; } = CountryPackStatus.Active;

    public bool IsInstalled { get; set; }

    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    public ICollection<TaxSlab> TaxSlabs { get; set; } = new List<TaxSlab>();
}
