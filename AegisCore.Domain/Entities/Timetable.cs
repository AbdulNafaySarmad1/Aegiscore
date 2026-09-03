using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("Timetables")]
public class Timetable : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    // FK
    public Guid BatchId { get; init; }
    public Guid CourseId { get; init; }
    public Guid FacultyId { get; init; }

    public DayOfWeek DayOfWeek { get; init; }

    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }

    [MaxLength(100)]
    public string? Room { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(BatchId))]
    public virtual Batch Batch { get; init; } = null!;

    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; init; } = null!;

    [ForeignKey(nameof(FacultyId))]
    public virtual Faculty Faculty { get; init; } = null!;
}
