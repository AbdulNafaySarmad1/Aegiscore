using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

// Auto Class Generation & LMS Integration
[Table("ClassGenerationJobs")] public class ClassGenerationJob : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(20)] public string AcademicYear { get; set; } = ""; [MaxLength(20)] public string Semester { get; set; } = "";
    public int TotalOfferings { get; set; } public int SectionsGenerated { get; set; }
    public int StudentsDistributed { get; set; } public int FacultyAssigned { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "pending";
    public DateTime? TriggeredAt { get; set; } = DateTime.UtcNow; public DateTime? CompletedAt { get; set; } public Guid TenantId { get; set; } }

[Table("Classes")] public class Class : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CourseOfferingId { get; set; } public Guid CourseId { get; set; }
    public int SectionNumber { get; set; } [MaxLength(200)] public string? SectionLabel { get; set; }
    [MaxLength(20)] public string AcademicYear { get; set; } = ""; [MaxLength(20)] public string Semester { get; set; } = "";
    public Guid? PrimaryFacultyId { get; set; }
    [Column(TypeName = "jsonb")] public string CoFacultyIdsJson { get; set; } = "[]";
    [Column(TypeName = "jsonb")] public string EnrolledStudentIdsJson { get; set; } = "[]";
    public int Capacity { get; set; } public int EnrolledCount { get; set; }
    public ClassGenerationStatus Status { get; set; } = ClassGenerationStatus.Draft;
    public Guid? GenerationJobId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("StudentSectionEnrollments")] public class StudentSectionEnrollment : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ClassId { get; set; } public Guid StudentId { get; set; } public Guid CourseOfferingId { get; set; }
    public int SectionNumber { get; set; } [MaxLength(20)] public string? DistributionStrategy { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("LmsProvisions")] public class LmsProvision : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ClassId { get; set; } public LmsProvider Provider { get; set; }
    [MaxLength(200)] public string? ExternalId { get; set; } [MaxLength(500)] public string? ExternalUrl { get; set; }
    public LmsProvisionStatus Status { get; set; } = LmsProvisionStatus.Pending;
    public DateTime? LastSyncAt { get; set; } public DateTime? ProvisionedAt { get; set; }
    [Column(TypeName = "jsonb")] public string ErrorLogJson { get; set; } = "{}";
    public int RetryCount { get; set; } public Guid TenantId { get; set; } }

[Table("LmsRosterSyncLogs")] public class LmsRosterSyncLog : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid ProvisionId { get; set; } public RosterSyncStatus Status { get; set; }
    public int MembersAdded { get; set; } public int MembersRemoved { get; set; }
    public int ConflictsDetected { get; set; } [Column(TypeName = "jsonb")] public string ConflictDetailsJson { get; set; } = "[]";
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("LmsConfigurations")] public class LmsConfiguration : ITenantScoped { [Key] public Guid Id { get; set; }
    public LmsProvider Provider { get; set; } public bool IsEnabled { get; set; }
    public bool IsPrimary { get; set; } [MaxLength(50)] public string? CoexistenceMode { get; set; }
    public int RetryAttempts { get; set; } = 3; public int RetryDelayMs { get; set; } = 5000;
    [MaxLength(50)] public string? SyncSchedule { get; set; } [MaxLength(50)] public string? ConflictResolution { get; set; }
    public int ConflictAlertThreshold { get; set; } = 10;
    [Column(TypeName = "jsonb")] public string ProviderConfigJson { get; set; } = "{}";
    public Guid TenantId { get; set; } }

[Table("DirectorySyncLogs")] public class DirectorySyncLog : ITenantScoped { [Key] public Guid Id { get; set; }
    public DirectoryProvider Provider { get; set; } public int UsersSynced { get; set; }
    public int UsersCreated { get; set; } public int UsersUpdated { get; set; } public int Errors { get; set; }
    [Column(TypeName = "jsonb")] public string ErrorDetailsJson { get; set; } = "[]";
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }
