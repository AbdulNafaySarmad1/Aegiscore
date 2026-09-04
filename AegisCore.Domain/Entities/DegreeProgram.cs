using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("DegreePrograms")]
public class DegreeProgram : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(50)]
    public string DegreeType { get; set; } = string.Empty;

    public int DurationYears { get; set; } = 4;

    public int MinCreditHours { get; set; } = 120;

    public decimal MinCGPA { get; set; } = 2.0m;

    public CurriculumVersion CurriculumVersion { get; set; } = CurriculumVersion.V2025;

    public Guid DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    public ICollection<DegreeRequirement> Requirements { get; set; } = new List<DegreeRequirement>();
    public ICollection<DegreeAudit> Audits { get; set; } = new List<DegreeAudit>();
}
