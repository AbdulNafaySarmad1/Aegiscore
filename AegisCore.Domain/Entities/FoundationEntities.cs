using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("PolicyConfigs")]
public class PolicyConfig : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string Module { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ValueType { get; set; } = "string";

    public string? Description { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EffectiveTo { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

[Table("AcademicYears")]
public class AcademicYear : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Semester> Semesters { get; set; } = new List<Semester>();
}

[Table("Semesters")]
public class Semester : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; set; }

    public Guid AcademicYearId { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Type { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly? ExamStartDate { get; set; }
    public DateOnly? ExamEndDate { get; set; }
    public DateOnly? ResultDeclarationDate { get; set; }
    public DateOnly? AddDropEndDate { get; set; }
    public bool IsCurrent { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "upcoming";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public AcademicYear AcademicYear { get; set; } = null!;
    public ICollection<AcademicHoliday> Holidays { get; set; } = new List<AcademicHoliday>();
}

[Table("AcademicHolidays")]
public class AcademicHoliday : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; set; }

    public Guid? SemesterId { get; set; }
    public Guid AcademicYearId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    public Semester? Semester { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
}

[Table("CrossDeptPermissionRequests")]
public class CrossDeptPermissionRequest : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; set; }

    public Guid RequestingUserId { get; set; }
    public Guid RequestingDeptId { get; set; }
    public Guid TargetDeptId { get; set; }
    public Guid PermissionId { get; set; }

    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "pending";

    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Permission Permission { get; set; } = null!;
}
