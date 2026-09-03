using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

// Course Catalog Ingestion Engine
[Table("IngestionJobs")] public class IngestionJob : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(300)] public string DocumentName { get; set; } = ""; [MaxLength(50)] public string? DocumentType { get; set; }
    [MaxLength(500)] public string? OriginalDocumentUrl { get; set; }
    public IngestionJobStatus Status { get; set; } = IngestionJobStatus.Received;
    [Column(TypeName = "text")] public string? RawExtraction { get; set; }
    public double ExtractionConfidence { get; set; }
    [Column(TypeName = "jsonb")] public string ClassificationLogJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string ReviewLogJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string InsertionLogJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string ErrorLogJson { get; set; } = "{}";
    public int CoursesExtracted { get; set; } public int CoursesInserted { get; set; }
    public int CoursesUpdated { get; set; } public int CoursesSkipped { get; set; }
    public int CoursesRequiringReview { get; set; } public int Warnings { get; set; } public int Errors { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow; public DateTime? CompletedAt { get; set; } public Guid TenantId { get; set; } }

[Table("CatalogCourseRecords")] public class CatalogCourseRecord : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid JobId { get; set; } [MaxLength(200)] public string DegreeProgram { get; set; } = "";
    [MaxLength(200)] public string? DegreeProgramSlug { get; set; }
    public int SemesterSequence { get; set; } [MaxLength(100)] public string? SemesterLabel { get; set; }
    [MaxLength(50)] public string CourseCode { get; set; } = ""; [MaxLength(50)] public string? CourseCodeNormalised { get; set; }
    [MaxLength(300)] public string CourseTitle { get; set; } = ""; [MaxLength(300)] public string? CourseTitleNormalised { get; set; }
    public int CreditHoursTheory { get; set; } public int CreditHoursLab { get; set; }
    public int CreditHoursTotal { get; set; }
    [Column(TypeName = "jsonb")] public string PrerequisitesJson { get; set; } = "[]";
    [MaxLength(50)] public string? CourseType { get; set; } [MaxLength(100)] public string? ElectiveSlot { get; set; }
    public CourseRecordStatus Status { get; set; } = CourseRecordStatus.Active;
    [Column(TypeName = "jsonb")] public string AnnotationsJson { get; set; } = "[]";
    public double ExtractionConfidence { get; set; } public bool RequiresReview { get; set; }
    [MaxLength(500)] public string? ReviewReason { get; set; }
    public bool IsUpdateCandidate { get; set; } public bool IsInserted { get; set; }
    public Guid TenantId { get; set; } }

[Table("CatalogVersions")] public class CatalogVersion : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid DegreeProgramId { get; set; } public int Version { get; set; }
    public CatalogPublishStatus Status { get; set; } = CatalogPublishStatus.Draft;
    public Guid? IngestionJobId { get; set; } [Column(TypeName = "jsonb")] public string SnapshotJson { get; set; } = "{}";
    [Column(TypeName = "jsonb")] public string ChangeLogJson { get; set; } = "{}";
    public DateTime? PublishedAt { get; set; } public Guid? PublishedByUserId { get; set; } public Guid TenantId { get; set; } }

[Table("DegreeTrees")] public class DegreeTree : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid DegreeProgramId { get; set; } public int CatalogVersion { get; set; }
    [Column(TypeName = "jsonb")] public string NodesJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string EdgesJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string CriticalPathJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string SemesterLanesJson { get; set; } = "[]";
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("ElectiveSlots")] public class ElectiveSlot : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid DegreeProgramId { get; set; } [MaxLength(100)] public string SlotLabel { get; set; } = "";
    [MaxLength(50)] public string? SlotCategory { get; set; } public int SemesterSequence { get; set; }
    public int CreditHoursTheory { get; set; } public int CreditHoursLab { get; set; }
    [Column(TypeName = "jsonb")] public string EligibleCourseIdsJson { get; set; } = "[]";
    [MaxLength(5)] public string? MinGradeRequired { get; set; } public bool IsStudentChoice { get; set; }
    public bool RequiresApproval { get; set; } public Guid TenantId { get; set; } }

[Table("CourseOfferings")] public class CourseOffering : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; } [MaxLength(20)] public string AcademicYear { get; set; } = "";
    [MaxLength(20)] public string Semester { get; set; } = ""; public int SectionNumber { get; set; }
    public int Capacity { get; set; } public int EnrolledCount { get; set; }
    public Guid? CampusId { get; set; } public ClassGenerationStatus Status { get; set; } = ClassGenerationStatus.Draft;
    public Guid TenantId { get; set; } }

[Table("CourseGradePolicies")] public class CourseGradePolicy : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; } [MaxLength(50)] public string GradingScheme { get; set; } = "";
    [MaxLength(5)] public string PassingGrade { get; set; } = "D"; public double GpaWeight { get; set; }
    public bool CanRepeat { get; set; } public int? MaxRepeatAttempts { get; set; }
    [MaxLength(20)] public string? GradeReplacement { get; set; } public Guid TenantId { get; set; } }

[Table("CourseEquivalencies")] public class CourseEquivalency : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid SourceCourseId { get; set; } public Guid TargetCourseId { get; set; }
    [MaxLength(200)] public string? SourceInstitution { get; set; } public bool IsApproved { get; set; }
    [MaxLength(500)] public string? Notes { get; set; } public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; } public Guid TenantId { get; set; } }
