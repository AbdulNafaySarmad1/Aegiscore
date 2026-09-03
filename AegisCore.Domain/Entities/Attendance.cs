using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("Attendances")]
public class Attendance : ITenantScoped
{
    public Guid TenantId { get; set; }
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
    public double Percentage { get; set; } = 0.0;

    public void UpdatePercentage(int totalSessions, int presentSessions)
    {
        Percentage = totalSessions > 0 ? Math.Round((double)presentSessions / totalSessions * 100.0, 2) : 0.0;
    }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(EnrollmentId))]
    public virtual Enrollment Enrollment { get; init; } = null!;

    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; init; } = null!;

    [ForeignKey(nameof(FacultyId))]
    public virtual Faculty Faculty { get; init; } = null!;
}
