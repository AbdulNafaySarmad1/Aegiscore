using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

/// <summary>
/// An admission intake window (year + semester). The admin sets the semester code (F/S/W)
/// per intake; it feeds the Applicant ID and, later, the roll number.
/// </summary>
[Table("AdmissionIntakes")]
public class AdmissionIntake : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public int Year { get; set; }
    public IntakeSemester Semester { get; set; }
    public IntakeStatus Status { get; set; } = IntakeStatus.Draft;

    [MaxLength(150)] public string Name { get; set; } = string.Empty;

    public DateTimeOffset? OpensAt { get; set; }
    public DateTimeOffset? ClosesAt { get; set; }

    /// <summary>Batch that students admitted through this intake are enrolled into.</summary>
    public Guid? BatchId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Single-letter semester code used in generated identifiers.</summary>
    [NotMapped]
    public string SemesterCode => Semester switch
    {
        IntakeSemester.Fall => "F",
        IntakeSemester.Spring => "S",
        IntakeSemester.Winter => "W",
        _ => "F"
    };
}

/// <summary>
/// A prospective applicant. Created on email OTP verification, before any institutional
/// account exists. Carries the unique Applicant ID for its whole lifetime.
/// </summary>
[Table("ApplicantProfiles")]
public class ApplicantProfile : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    [Required, MaxLength(40)] public string ApplicantId { get; set; } = string.Empty;

    [Required, MaxLength(320)] public string Email { get; set; } = string.Empty;
    public DateTimeOffset? EmailVerifiedAt { get; set; }

    public Guid IntakeId { get; set; }
    public Guid ProgramId { get; set; }

    public ApplicantStatus Status { get; set; } = ApplicantStatus.Draft;

    [MaxLength(200)] public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(20)] public string? Gender { get; set; }
    [MaxLength(100)] public string? Nationality { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }

    [MaxLength(1000)] public string? DecisionNote { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Set once the applicant is admitted and converted to a Student. Never cleared.</summary>
    public Guid? ConvertedStudentId { get; set; }

    public ICollection<ApplicantDocument> Documents { get; set; } = [];
}

/// <summary>
/// Admin-configured document requirement for a given intake + program. The applicant upload
/// page renders exactly the rows defined here — nothing is hardcoded.
/// </summary>
[Table("AdmissionDocumentRequirements")]
public class AdmissionDocumentRequirement : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public Guid IntakeId { get; set; }
    public Guid ProgramId { get; set; }

    public AdmissionDocumentKind DocumentType { get; set; }
    public bool IsRequired { get; set; } = true;
    public AdmissionDocumentFormat AcceptedFormat { get; set; } = AdmissionDocumentFormat.Pdf;
    public int MaxFileSizeMb { get; set; } = 5;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// A file uploaded by an applicant. Stored in the existing vault/storage layer
/// (<c>IStorageProvider</c>); this row is the metadata.
/// </summary>
[Table("ApplicantDocuments")]
public class ApplicantDocument : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }

    public Guid ApplicantId { get; set; }

    public AdmissionDocumentKind DocumentType { get; set; }

    [Required, MaxLength(600)] public string StoragePath { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public long FileSize { get; set; }
    [Required, MaxLength(100)] public string MimeType { get; set; } = string.Empty;

    public AdmissionDocumentValidationStatus ValidationStatus { get; set; } = AdmissionDocumentValidationStatus.Pending;
    [MaxLength(1000)] public string? RejectionReason { get; set; }

    /// <summary>CLIP confidence for a passport photo; null for certificates or when the check failed open.</summary>
    public double? ValidationConfidence { get; set; }
    /// <summary>"clip", "failopen", or "manual".</summary>
    [MaxLength(30)] public string? ValidationMethod { get; set; }

    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public ApplicantProfile Applicant { get; set; } = null!;
}
