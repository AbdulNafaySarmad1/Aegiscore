using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Batches")]
public class Batch
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string Name { get; init; }

    public int StartYear { get; init; }
    public int EndYear { get; init; }

    // FK
    public Guid DepartmentId { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(DepartmentId))]
    public virtual Department Department { get; init; } = null!;

    public virtual ICollection<Student> Students { get; init; } = new List<Student>();
    public virtual ICollection<Timetable> Timetables { get; init; } = new List<Timetable>();
}
