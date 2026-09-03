using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("CurriculumVersions")]
public class CurriculumVersionRecord : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public int VersionNumber { get; set; }
    public int YearEffective { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "draft";
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ApprovedAt { get; set; }
    [MaxLength(4000)] public string? Changelog { get; set; }
    public ICollection<CurriculumCourse> Courses { get; set; } = [];
    public ICollection<CurriculumPrerequisite> Prerequisites { get; set; } = [];
}

[Table("CurriculumCourses")]
public class CurriculumCourse : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid CurriculumVersionId { get; set; }
    public Guid CourseId { get; set; }
    public int Semester { get; set; }
    public bool IsMandatory { get; set; } = true;
    public bool IsElective { get; set; }
    public int TheoryCreditHours { get; set; }
    public int LabCreditHours { get; set; }
    public int TotalCreditHours { get; private set; }
    [MaxLength(1000)] public string MappedClosCsv { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public CurriculumVersionRecord CurriculumVersion { get; set; } = null!;
}

[Table("CurriculumPrerequisites")]
public class CurriculumPrerequisite : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid CurriculumVersionId { get; set; }
    public Guid CourseId { get; set; }
    public Guid PrerequisiteCourseId { get; set; }
    [Required, MaxLength(20)] public string Relationship { get; set; } = "prerequisite";
    [MaxLength(1000)] public string SubstituteCourseIdsCsv { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public CurriculumVersionRecord CurriculumVersion { get; set; } = null!;
}

[Table("CurriculumChangeHistory")]
public class CurriculumChangeHistory : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid CurriculumVersionId { get; set; }
    [Required, MaxLength(40)] public string ChangeType { get; set; } = string.Empty;
    public Guid? CourseId { get; set; }
    [Column(TypeName = "jsonb")] public string? BeforeValueJson { get; set; }
    [Column(TypeName = "jsonb")] public string? AfterValueJson { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(1000)] public string? Reason { get; set; }
}

[Table("OfflineAttendanceEvents")]
public class OfflineAttendanceEvent : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(100)] public string ClientEventId { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }
    public Guid MarkedBy { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "present";
    public DateTimeOffset WasOfflineAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SyncedAt { get; set; }
}
