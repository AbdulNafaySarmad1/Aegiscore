using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

/// <summary>
/// Represents a student's result for a course.
/// </summary>
[Table("Results")]
public class Result : ITenantScoped
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Enrollment identifier.
    /// </summary>
    public Guid EnrollmentId { get; init; }

    /// <summary>
    /// Course identifier.
    /// </summary>
    public Guid CourseId { get; init; }

    /// <summary>
    /// Total marks for the assessment.
    /// </summary>
    public decimal TotalMarks { get; init; }

    /// <summary>
    /// Marks obtained by the student.
    /// </summary>
    public decimal ObtainedMarks { get; init; }

    /// <summary>
    /// Tenant identifier for multi-tenancy.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Auto-calculated grade (not persisted).
    /// </summary>
    [NotMapped]
    public string Grade => CalculateGrade();

    /// <summary>
    /// Timestamp when the result was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the enrollment.
    /// </summary>
    [ForeignKey(nameof(EnrollmentId))]
    public virtual Enrollment Enrollment { get; init; } = null!;

    /// <summary>
    /// Navigation property to the course.
    /// </summary>
    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; init; } = null!;

    private string CalculateGrade()
    {
        if (TotalMarks <= 0) return "N/A";
        double percentage = (double)ObtainedMarks / (double)TotalMarks * 100;
        return percentage switch
        {
            >= 90 => "A+",
            >= 80 => "A",
            >= 70 => "B",
            >= 60 => "C",
            >= 50 => "D",
            _ => "F"
        };
    }
}
