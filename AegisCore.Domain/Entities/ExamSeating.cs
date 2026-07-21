using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ExamSeatings")]
public class ExamSeating
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    // FK
    public Guid ExamScheduleId { get; init; }
    public Guid StudentId { get; init; }

    public int SeatNumber { get; init; }

    [MaxLength(50)]
    public string? Classroom { get; init; }

    public int RowNumber { get; init; }
    public int ColumnNumber { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(ExamScheduleId))]
    public virtual ExamSchedule ExamSchedule { get; init; } = null!;

    [ForeignKey(nameof(StudentId))]
    public virtual Student Student { get; init; } = null!;
}
