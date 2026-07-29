using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("CloPlomappings")]
public class CloPlomapping
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    // FK
    public Guid CourseId { get; init; }

    [Required]
    [MaxLength(50)]
    public required string CloCode { get; init; }

    [Required]
    [MaxLength(50)]
    public required string PloCode { get; init; }

    public AlignmentLevel AlignmentLevel { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; init; } = null!;
}
