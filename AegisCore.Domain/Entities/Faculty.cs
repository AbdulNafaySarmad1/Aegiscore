using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Faculties")]
public class Faculty : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }

    [MaxLength(50)]
    public string? EmployeeCode { get; init; }

    public Designation Designation { get; init; }

    // FK
    public Guid DepartmentId { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(DepartmentId))]
    public virtual Department Department { get; init; } = null!;

    public virtual ICollection<CourseAssignment> CourseAssignments { get; init; } = new List<CourseAssignment>();
    public virtual ICollection<Attendance> Attendances { get; init; } = new List<Attendance>();
    public virtual ICollection<Timetable> Timetables { get; init; } = new List<Timetable>();
}
