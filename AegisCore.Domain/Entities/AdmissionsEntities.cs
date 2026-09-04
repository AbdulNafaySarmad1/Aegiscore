using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("AdmissionApplications")]
public class AdmissionApplication : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid? EnquiryLeadId { get; set; }
    [Required, MaxLength(200)] public string StudentName { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Phone { get; set; } = string.Empty;
    [Required, MaxLength(15)] public string Cnic { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    [MaxLength(20)] public string? Gender { get; set; }
    public Guid ProgramId { get; set; }
    [Required, MaxLength(50)] public string QuotaType { get; set; } = "open_merit";
    [Column(TypeName = "decimal(5,2)")] public decimal? MatricPercentage { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? FscPercentage { get; set; }
    public int? EntryTestScore { get; set; }
    public bool IbccEquivalenceApplied { get; set; }
    [Required, MaxLength(20)] public string ApplicationStatus { get; set; } = "submitted";
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ApplicationDocument> Documents { get; set; } = [];
}

[Table("ApplicationDocuments")]
public class ApplicationDocument : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    [Required, MaxLength(50)] public string DocumentType { get; set; } = string.Empty;
    [Required] public string FilePath { get; set; } = string.Empty;
    public DateTimeOffset UploadDate { get; set; } = DateTimeOffset.UtcNow;
    public Guid? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    [Required, MaxLength(20)] public string VerificationStatus { get; set; } = "pending";
    [MaxLength(1000)] public string? RejectionReason { get; set; }
    public AdmissionApplication Application { get; set; } = null!;
}

[Table("MeritLists")]
public class MeritList : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public Guid SemesterId { get; set; }
    [Required, MaxLength(50)] public string QuotaType { get; set; } = "open_merit";
    public int RoundNumber { get; set; } = 1;
    public int TotalSeats { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid GeneratedBy { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "draft";
    public DateTimeOffset? PublishedAt { get; set; }
    public ICollection<MeritListEntry> Entries { get; set; } = [];
}

[Table("MeritListEntries")]
public class MeritListEntry : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid MeritListId { get; set; }
    public Guid ApplicationId { get; set; }
    public int MeritRank { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal MeritScore { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal MatricContribution { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal FscContribution { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal EntryTestContribution { get; set; }
    public bool SeatAllocated { get; set; }
    [MaxLength(20)] public string? AllocationStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public MeritList MeritList { get; set; } = null!;
    public AdmissionApplication Application { get; set; } = null!;
}

[Table("SeatAllocations")]
public class SeatAllocation : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid MeritListEntryId { get; set; }
    public Guid ApplicationId { get; set; }
    [Required, MaxLength(50)] public string SeatType { get; set; } = "regular";
    public DateTimeOffset AllocatedAt { get; set; } = DateTimeOffset.UtcNow;
}
