using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

// GAP 01 — Finance / General Ledger

[Table("ChartOfAccounts")] public class ChartOfAccount : ITenantScoped { [Key] public Guid Id { get; set; }
    [Required][MaxLength(50)] public string AccountCode { get; set; } = ""; [MaxLength(200)] public string AccountName { get; set; } = "";
    [MaxLength(20)] public string? CashFlowCategory { get; set; }
    public AccountType AccountType { get; set; } public Guid? ParentAccountId { get; set; }
    public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("JournalEntries")] public class JournalEntry : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(50)] public string EntryNumber { get; set; } = ""; public JournalEntryType EntryType { get; set; }
    [MaxLength(1000)] public string? Description { get; set; } public DateTime EntryDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalDebit { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalCredit { get; set; }
    public bool IsPosted { get; set; } public bool IsSandbox { get; set; } public Guid? PostedByUserId { get; set; } public DateTime? PostedAt { get; set; }
    public byte[]? PreviousHash { get; set; } [MaxLength(3000)] public string? CanonicalPayload { get; set; } public byte[]? EntryHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<JournalEntryLine> Lines { get; set; } = []; }

[Table("JournalEntryLines")] public class JournalEntryLine : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; } public Guid ChartOfAccountId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ReportingEntityId { get; set; } public Guid? CounterpartyReportingEntityId { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Debit { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Credit { get; set; }
    [MaxLength(500)] public string? Description { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(JournalEntryId))] public JournalEntry JournalEntry { get; set; } = null!; }

[Table("FiscalPeriods")] public class FiscalPeriod : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid FiscalYearId { get; set; } [MaxLength(50)] public string Name { get; set; } = "";
    public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public bool IsClosed { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(FiscalYearId))] public FiscalYear FiscalYear { get; set; } = null!; }

[Table("FiscalYears")] public class FiscalYear : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(50)] public string Name { get; set; } = ""; public DateTime StartDate { get; set; } public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; } public Guid TenantId { get; set; }
    public ICollection<FiscalPeriod> Periods { get; set; } = []; }

// GAP 02 — HR ERP

[Table("Employees")] public class Employee { [Key] public Guid Id { get; set; }
    [MaxLength(100)] public string FirstName { get; set; } = ""; [MaxLength(100)] public string LastName { get; set; } = "";
    [MaxLength(50)] public string? EmployeeCode { get; set; } [MaxLength(300)] public string? Email { get; set; }
    public EmployeeType EmployeeType { get; set; } public EmployeeStatus Status { get; set; }
    public Designation Designation { get; set; } public Guid DepartmentId { get; set; }
    public DateTime? DateOfJoining { get; set; } public DateTime? DateOfLeaving { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? Salary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<EmployeeDocument> Documents { get; set; } = []; public ICollection<EmployeeContract> Contracts { get; set; } = []; }

[Table("EmployeeDocuments")] public class EmployeeDocument { [Key] public Guid Id { get; set; }
    public Guid EmployeeId { get; set; } [MaxLength(200)] public string DocumentType { get; set; } = "";
    [MaxLength(500)] public string? FileUrl { get; set; } public DateTime UploadedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!; }

[Table("EmployeeContracts")] public class EmployeeContract { [Key] public Guid Id { get; set; }
    public Guid EmployeeId { get; set; } public DateTime StartDate { get; set; } public DateTime? EndDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? Salary { get; set; } [MaxLength(200)] public string? ContractType { get; set; }
    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("InterviewSchedules")] public class InterviewSchedule { [Key] public Guid Id { get; set; }
    public Guid JobApplicationId { get; set; } public DateTime ScheduledAt { get; set; }
    public InterviewStatus Status { get; set; } public InterviewType Type { get; set; }
    public Guid TenantId { get; set; } }

[Table("LeaveApplications")] public class LeaveApplication { [Key] public Guid Id { get; set; }
    public Guid EmployeeId { get; set; } public LeaveType LeaveType { get; set; } public LeaveStatus Status { get; set; }
    public DateTime StartDate { get; set; } public DateTime EndDate { get; set; }
    [MaxLength(1000)] public string? Reason { get; set; } public Guid? ApprovedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("AttendanceRecords")] public class AttendanceRecord { [Key] public Guid Id { get; set; }
    public Guid EmployeeId { get; set; } public AttendanceMethod Method { get; set; }
    public DateTime CheckIn { get; set; } public DateTime? CheckOut { get; set; }
    public AttendanceStatus Status { get; set; } public Guid TenantId { get; set; } }

[Table("PerformanceReviews")] public class PerformanceReview { [Key] public Guid Id { get; set; }
    public Guid EmployeeId { get; set; } public ReviewType ReviewType { get; set; } public ReviewStatus Status { get; set; }
    public DateTime ReviewPeriodStart { get; set; } public DateTime ReviewPeriodEnd { get; set; }
    public int? OverallRating { get; set; } [MaxLength(2000)] public string? Comments { get; set; }
    public Guid ReviewedByUserId { get; set; } public Guid TenantId { get; set; } }

[Table("TrainingAssignments")] public class TrainingAssignment { [Key] public Guid Id { get; set; }
    public Guid EmployeeId { get; set; } [MaxLength(300)] public string TrainingTitle { get; set; } = "";
    public TrainingStatus Status { get; set; } public DateTime? AssignedDate { get; set; } public DateTime? CompletedDate { get; set; }
    [MaxLength(100)] public string? Provider { get; set; } public Guid TenantId { get; set; } }

// GAP 01 — SIS entities (referenced in DbContext)
[Table("StudentProfiles")] public class StudentProfile { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [Column(TypeName = "jsonb")] public string ProfileJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("StudentDocuments")] public class StudentDocument { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(200)] public string DocumentType { get; set; } = "";
    [MaxLength(500)] public string? FileUrl { get; set; } public DateTime UploadedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("StudentFees")] public class StudentFee { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public FeeStatus Status { get; set; } public DateTime DueDate { get; set; }
    public ICollection<FeePayment> Payments { get; set; } = []; public Guid TenantId { get; set; } }

[Table("FeePayments")] public class FeePayment { [Key] public Guid Id { get; set; }
    public Guid StudentFeeId { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow; [MaxLength(50)] public string? PaymentMethod { get; set; }
    [ForeignKey(nameof(StudentFeeId))] public StudentFee StudentFee { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("StudentDisciplines")] public class StudentDiscipline { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public DisciplineSeverity Severity { get; set; } public DisciplineStatus Status { get; set; }
    [MaxLength(2000)] public string? Description { get; set; } public DateTime IncidentDate { get; set; } public Guid TenantId { get; set; } }

[Table("StudentCounselings")] public class StudentCounseling { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(2000)] public string? Notes { get; set; }
    public DateTime SessionDate { get; set; } public Guid CounselorUserId { get; set; } public Guid TenantId { get; set; } }

[Table("StudentAchievements")] public class StudentAchievement { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public AchievementType Type { get; set; } [MaxLength(500)] public string? Description { get; set; }
    public DateTime AchievedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("StudentScholarships")] public class StudentScholarship { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(200)] public string ScholarshipName { get; set; } = "";
    public ScholarshipStatus Status { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public DateTime AwardedAt { get; set; } public DateTime? ExpiresAt { get; set; } public Guid TenantId { get; set; } }

[Table("StudentHostels")] public class StudentHostel { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid HostelRoomId { get; set; } public HostelStatus Status { get; set; }
    public DateTime CheckInDate { get; set; } public DateTime? CheckOutDate { get; set; } public Guid TenantId { get; set; } }

[Table("StudentTransports")] public class StudentTransport { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid TransportRouteId { get; set; } public TransportStatus Status { get; set; }
    public DateTime StartDate { get; set; } public DateTime? EndDate { get; set; } public Guid TenantId { get; set; } }

[Table("StudentGuardians")] public class StudentGuardian { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(50)] public string? Relationship { get; set; }
    [MaxLength(50)] public string? Phone { get; set; } [MaxLength(300)] public string? Email { get; set; } public Guid TenantId { get; set; } }

[Table("StudentClearances")] public class StudentClearance { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public ClearanceStatus Status { get; set; }
    [MaxLength(100)] public string? Department { get; set; } public Guid? ClearedByUserId { get; set; }
    public DateTime? ClearedAt { get; set; } public Guid TenantId { get; set; } }
