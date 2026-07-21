using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Campuses")]
public class Campus
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CampusCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Email { get; set; }

    public CampusStatus Status { get; set; } = CampusStatus.Active;

    public CalendarType CalendarType { get; set; } = CalendarType.Semester;

    public GradingScheme GradingScheme { get; set; } = GradingScheme.GPA4;

    [MaxLength(50)]
    public string? Timezone { get; set; } = "UTC";

    [MaxLength(10)]
    public string? Language { get; set; } = "en";

    public bool IsMain { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    public ICollection<Building> Buildings { get; set; } = new List<Building>();
}
