using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Attendances")]
public class Attendance
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    // FK
    public Guid EnrollmentId { get; init; }
    public Guid CourseId { get; init; }
    public Guid FacultyId { get; init; }

    public DateTime Date { get; init; }

    public AttendanceStatus Status { get; init; }

    // Calculated property (not mapped)
    [NotMapped]
    public double Percentage => CalculatePercentage();

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(EnrollmentId))]
    public virtual Enrollment Enrollment { get; init; } = null!;

    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; init; } = null!;

    [ForeignKey(nameof(FacultyId))]
    public virtual Faculty Faculty { get; init; } = null!;

    private double CalculatePercentage()
    {
        // Placeholder – real calculation would need total sessions context
        return Status == AttendanceStatus.Present ? 100.0 : 0.0;
    }
}
