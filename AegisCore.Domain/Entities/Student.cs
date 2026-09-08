using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Students")]
public class Student : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [MaxLength(50)]
    public string? UniversityId { get; set; }

    /// <summary>Assigned roll number: {YEAR}{SEM}-{PROGRAM_CODE}-{SEQUENCE}, e.g. 2025F-BCS-181.</summary>
    [MaxLength(50)]
    public string? RollNumber { get; set; }

    /// <summary>The Applicant ID this student was admitted under. Retained for audit — never deleted.</summary>
    [MaxLength(40)]
    public string? ApplicantId { get; set; }

    public Guid? AdmissionApplicantProfileId { get; set; }

    public EnrollmentStatus EnrollmentStatus { get; init; }
    public bool DirectoryOptOut { get; set; }


    // FK
    public Guid BatchId { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(BatchId))]
    public virtual Batch Batch { get; init; } = null!;

    public virtual ICollection<Enrollment> Enrollments { get; init; } = new List<Enrollment>();
    public virtual ICollection<ExamSeating> ExamSeatings { get; init; } = new List<ExamSeating>();
}
