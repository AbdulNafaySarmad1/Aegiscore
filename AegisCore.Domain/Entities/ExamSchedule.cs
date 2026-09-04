using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ExamSchedules")]
public class ExamSchedule : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    // FK
    public Guid CourseId { get; init; }

    public ExamType ExamType { get; init; }

    public DateTime ScheduledDateTime { get; init; }

    [MaxLength(100)]
    public string? Venue { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; init; } = null!;

    public virtual ICollection<ExamSeating> ExamSeatings { get; init; } = new List<ExamSeating>();
}
