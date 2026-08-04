using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("CourseAssignments")]
public class CourseAssignment
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    // FK
    public Guid CourseId { get; init; }
    public Guid FacultyId { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; init; } = null!;

    [ForeignKey(nameof(FacultyId))]
    public virtual Faculty Faculty { get; init; } = null!;
}
