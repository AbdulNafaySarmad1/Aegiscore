using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("Enrollments")]
public class Enrollment : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    // FK
    public Guid StudentId { get; init; }
    public Guid CourseId { get; init; }

    [MaxLength(5)]
    public string? Grade { get; init; }

    public decimal? GpaPoints { get; init; }

    public EnrollmentStatus EnrollmentStatus { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; init; } = null!;

    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; init; } = null!;

    public virtual ICollection<Attendance> Attendances { get; init; } = new List<Attendance>();
    public virtual ICollection<Result> Results { get; init; } = new List<Result>();
}
