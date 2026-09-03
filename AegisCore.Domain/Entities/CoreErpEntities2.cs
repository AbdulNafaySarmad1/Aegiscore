using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

// GAP 05 — Accreditation Suite

[Table("AccreditationBodies")] public class AccreditationBody : ITenantScoped { [Key] public Guid Id { get; set; }
    [Required][MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(50)] public string Code { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<AccreditationCriterion> Criteria { get; set; } = []; public ICollection<AccreditationCycle> Cycles { get; set; } = []; }

[Table("AccreditationCriteria")] public class AccreditationCriterion : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationBodyId { get; set; } [MaxLength(50)] public string Code { get; set; } = "";
    [MaxLength(500)] public string Title { get; set; } = ""; [MaxLength(2000)] public string? Description { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(AccreditationBodyId))] public AccreditationBody Body { get; set; } = null!;
    public ICollection<AccreditationEvidence> Evidences { get; set; } = []; }

[Table("AccreditationCycles")] public class AccreditationCycle : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationBodyId { get; set; } public Guid DepartmentId { get; set; }
    public AccreditationCycleStatus Status { get; set; } public DateTime? VisitDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(AccreditationBodyId))] public AccreditationBody Body { get; set; } = null!;
    public ICollection<SelfStudyReport> SelfStudyReports { get; set; } = []; public ICollection<AccreditationCommittee> Committees { get; set; } = []; }

[Table("AccreditationEvidences")] public class AccreditationEvidence : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationCriterionId { get; set; } public Guid DepartmentId { get; set; }
    [MaxLength(500)] public string? FileUrl { get; set; } [MaxLength(1000)] public string? Description { get; set; }
    public Guid UploadedByUserId { get; set; } public DateTime UploadedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(AccreditationCriterionId))] public AccreditationCriterion Criterion { get; set; } = null!;
    public ICollection<EvidenceVersion> Versions { get; set; } = []; }

[Table("EvidenceVersions")] public class EvidenceVersion : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationEvidenceId { get; set; } public int VersionNumber { get; set; }
    [MaxLength(500)] public string? FileUrl { get; set; } public Guid UploadedByUserId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(AccreditationEvidenceId))] public AccreditationEvidence Evidence { get; set; } = null!; }

[Table("AccreditationCriterionMappings")] public class AccreditationCriterionMapping : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationCriterionId { get; set; } public Guid? CourseId { get; set; } public Guid? ProgramOutcomeId { get; set; }
    [MaxLength(1000)] public string? MappingNotes { get; set; } public Guid TenantId { get; set; } }

[Table("FacultyCVs")] public class FacultyCV : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid FacultyId { get; set; } [Column(TypeName = "jsonb")] public string CvJson { get; set; } = "{}";
    public int Version { get; set; } = 1; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("LabInventories")] public class LabInventory : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid LabId { get; set; } [MaxLength(200)] public string ItemName { get; set; } = "";
    public int Quantity { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal? Value { get; set; }
    public DateTime LastVerifiedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("SelfStudyReports")] public class SelfStudyReport : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationCycleId { get; set; } public SSRStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? SubmittedAt { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(AccreditationCycleId))] public AccreditationCycle Cycle { get; set; } = null!;
    public ICollection<SSRSection> Sections { get; set; } = []; public ICollection<SSRReview> Reviews { get; set; } = []; }

[Table("SSRSections")] public class SSRSection : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid SelfStudyReportId { get; set; } [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(10000)] public string? Content { get; set; } public SSRSectionStatus Status { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(SelfStudyReportId))] public SelfStudyReport Report { get; set; } = null!; }

[Table("SSRReviews")] public class SSRReview : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid SelfStudyReportId { get; set; } public Guid ReviewedByUserId { get; set; }
    [MaxLength(2000)] public string? Comments { get; set; } public SSRReviewStatus Status { get; set; }
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(SelfStudyReportId))] public SelfStudyReport Report { get; set; } = null!; }

[Table("AccreditationCommittees")] public class AccreditationCommittee : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationCycleId { get; set; } [MaxLength(200)] public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(AccreditationCycleId))] public AccreditationCycle Cycle { get; set; } = null!;
    public ICollection<CommitteeMember> Members { get; set; } = []; public ICollection<CommitteeMeeting> Meetings { get; set; } = []; }

[Table("CommitteeMembers")] public class CommitteeMember : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationCommitteeId { get; set; } public Guid FacultyId { get; set; }
    public CommitteeRole Role { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(AccreditationCommitteeId))] public AccreditationCommittee Committee { get; set; } = null!; }

[Table("CommitteeMeetings")] public class CommitteeMeeting : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationCommitteeId { get; set; } public DateTime MeetingDate { get; set; }
    [MaxLength(5000)] public string? Minutes { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(AccreditationCommitteeId))] public AccreditationCommittee Committee { get; set; } = null!; }

[Table("ActionItems")] public class ActionItem : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid CommitteeMeetingId { get; set; } [MaxLength(500)] public string Title { get; set; } = "";
    public ActionItemStatus Status { get; set; } public Guid? AssignedToUserId { get; set; }
    public DateTime? DueDate { get; set; } public DateTime? CompletedAt { get; set; } public Guid TenantId { get; set; } }

[Table("AccreditationAuditLogs")] public class AccreditationAuditLog : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationCycleId { get; set; } [MaxLength(50)] public string Action { get; set; } = "";
    [MaxLength(500)] public string? Details { get; set; } public Guid PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(AccreditationCycleId))] public AccreditationCycle Cycle { get; set; } = null!; }

[Table("PreviousVisitFindings")] public class PreviousVisitFinding : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AccreditationCycleId { get; set; } public FindingType FindingType { get; set; }
    public FindingSeverity FindingSeverity { get; set; } [MaxLength(2000)] public string? Description { get; set; }
    public bool IsResolved { get; set; } public Guid TenantId { get; set; } }

// GAP 07 — Library ERP

[Table("LibraryBooks")] public class LibraryBook : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(500)] public string Title { get; set; } = ""; [MaxLength(200)] public string? Author { get; set; }
    [MaxLength(50)] public string? Isbn { get; set; } [MaxLength(200)] public string? Publisher { get; set; }
    public int PublicationYear { get; set; } [MaxLength(100)] public string? Category { get; set; }
    public int TotalCopies { get; set; } public int AvailableCopies { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<BookCopy> Copies { get; set; } = []; }

[Table("BookCopies")] public class BookCopy : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid LibraryBookId { get; set; } [MaxLength(100)] public string? Barcode { get; set; }
    public CopyStatus Status { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(LibraryBookId))] public LibraryBook Book { get; set; } = null!; }

[Table("LibraryMembers")] public class LibraryMember : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid UserId { get; set; } public MemberType MemberType { get; set; } public MemberStatus Status { get; set; }
    public DateTime MembershipExpiry { get; set; } public int MaxBooksAllowed { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("BookIssues")] public class BookIssue : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid LibraryMemberId { get; set; } public Guid BookCopyId { get; set; }
    public IssueStatus Status { get; set; } public DateTime IssueDate { get; set; } public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal FineAmount { get; set; }
    public Guid TenantId { get; set; } }

[Table("BookReservations")] public class BookReservation : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid LibraryMemberId { get; set; } public Guid LibraryBookId { get; set; }
    public ReservationStatus Status { get; set; } public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; } public Guid TenantId { get; set; } }

[Table("FinePayments")] public class FinePayment : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid BookIssueId { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("LibraryVendors")] public class LibraryVendor : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(300)] public string? Contact { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("BookOrders")] public class BookOrder : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid LibraryVendorId { get; set; } public OrderStatus Status { get; set; }
    public DateTime OrderDate { get; set; } public DateTime? DeliveryDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    public ICollection<OrderItem> Items { get; set; } = []; public Guid TenantId { get; set; } }

[Table("OrderItems")] public class OrderItem : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid BookOrderId { get; set; } [MaxLength(500)] public string Title { get; set; } = "";
    public int Quantity { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(BookOrderId))] public BookOrder Order { get; set; } = null!; }

[Table("LibraryBudgets")] public class LibraryBudget : ITenantScoped { [Key] public Guid Id { get; set; }
    public int FiscalYear { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal TotalBudget { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal SpentAmount { get; set; } public BudgetStatus Status { get; set; }
    public Guid TenantId { get; set; } }

[Table("LibraryUsages")] public class LibraryUsage : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid LibraryMemberId { get; set; } public DateTime EntryTime { get; set; } public DateTime? ExitTime { get; set; }
    public Guid TenantId { get; set; } }
