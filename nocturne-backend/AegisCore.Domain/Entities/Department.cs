using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Departments")]
public class Department
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [MaxLength(50)]
    public string? Code { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<Batch> Batches { get; init; } = new List<Batch>();
    public virtual ICollection<Faculty> Faculties { get; init; } = new List<Faculty>();
    public virtual ICollection<Course> Courses { get; init; } = new List<Course>();
}
