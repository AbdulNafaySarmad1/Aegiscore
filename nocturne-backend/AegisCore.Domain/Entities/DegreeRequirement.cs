using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("DegreeRequirements")]
public class DegreeRequirement
{
    [Key]
    public Guid Id { get; set; }

    public Guid ProgramId { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DegreeRequirementCategory Category { get; set; } = DegreeRequirementCategory.Core;

    public RequirementType RequirementType { get; set; } = RequirementType.CourseCompletion;

    public Guid? CourseId { get; set; }

    public int? MinCredits { get; set; }

    public int? CourseCount { get; set; }

    public decimal? MinGradePoints { get; set; }

    [Column(TypeName = "jsonb")]
    public string AllowedCourseIdsJson { get; set; } = "[]";

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(ProgramId))]
    public DegreeProgram Program { get; set; } = null!;
}
