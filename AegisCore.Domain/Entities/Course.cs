using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("Courses")]
public class Course : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public required string Title { get; init; }

    [MaxLength(20)]
    public string? Code { get; init; }

    public int CreditHours { get; init; }

    // FK
    public Guid DepartmentId { get; init; }

    // Self‑referencing FK for prerequisite
    public Guid? PrerequisiteCourseId { get; init; }

    // JSON‑like columns stored as string (EF Core will map to nvarchar)
    [Column(TypeName = "nvarchar(max)")]
    public string? CloMappingJson { get; init; }

    [Column(TypeName = "nvarchar(max)")]
    public string? PloMappingJson { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(DepartmentId))]
    public virtual Department Department { get; init; } = null!;

    [ForeignKey(nameof(PrerequisiteCourseId))]
    public virtual Course? PrerequisiteCourse { get; init; }

    public virtual ICollection<Course> DependentCourses { get; init; } = new List<Course>();
    public virtual ICollection<CourseAssignment> CourseAssignments { get; init; } = new List<CourseAssignment>();
    public virtual ICollection<Enrollment> Enrollments { get; init; } = new List<Enrollment>();
    public virtual ICollection<Attendance> Attendances { get; init; } = new List<Attendance>();
    public virtual ICollection<ExamSchedule> ExamSchedules { get; init; } = new List<ExamSchedule>();
    public virtual ICollection<Result> Results { get; init; } = new List<Result>();
    public virtual ICollection<Timetable> Timetables { get; init; } = new List<Timetable>();
    public virtual ICollection<CloPlomapping> CloPlomappings { get; init; } = new List<CloPlomapping>();
}
