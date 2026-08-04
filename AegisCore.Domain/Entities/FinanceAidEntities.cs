using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceGrants")]
public sealed class FinanceGrant : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(200)] public string DonorName { get; set; } = string.Empty;
    [Required, MaxLength(3)] public string Currency { get; set; } = "PKR";
    [Column(TypeName = "numeric(19,4)")] public decimal AwardAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ReceivedAmount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal SpentAmount { get; set; }
    [Required, MaxLength(500)] public string Purpose { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "ACTIVE";
    public Guid TenantId { get; set; }
    public ICollection<FinanceScholarshipAward> Awards { get; set; } = [];
}

[Table("FinanceScholarshipPrograms")]
public sealed class FinanceScholarshipProgram : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid? GrantId { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string ScholarshipType { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string CoverageType { get; set; } = "FIXED_AMOUNT";
    [Column(TypeName = "numeric(19,4)")] public decimal CoverageValue { get; set; }
    [Required, MaxLength(20)] public string DisbursementMethod { get; set; } = "FEE_CREDIT";
    [Column(TypeName = "numeric(5,2)")] public decimal? MinimumCgpa { get; set; }
    public bool AllowsStacking { get; set; }
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public int DurationMonths { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "ANNOUNCED";
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(GrantId))] public FinanceGrant? Grant { get; set; }
    public ICollection<FinanceScholarshipApplication> Applications { get; set; } = [];
}

[Table("FinanceScholarshipApplications")]
public sealed class FinanceScholarshipApplication : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public Guid StudentId { get; set; }
    [Column(TypeName = "numeric(5,2)")] public decimal? Cgpa { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "APPLIED";
    [MaxLength(1000)] public string? ApplicantStatement { get; set; }
    [MaxLength(1000)] public string? DecisionNote { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public Guid? AwardId { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(ProgramId))] public FinanceScholarshipProgram Program { get; set; } = null!;
    public ICollection<FinanceScholarshipApplicationEvent> Events { get; set; } = [];
}

[Table("FinanceScholarshipApplicationEvents")]
public sealed class FinanceScholarshipApplicationEvent : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    [Required, MaxLength(20)] public string FromStatus { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string ToStatus { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Note { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(ApplicationId))] public FinanceScholarshipApplication Application { get; set; } = null!;
}

[Table("FinanceScholarshipAwards")]
public sealed class FinanceScholarshipAward : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid? GrantId { get; set; }
    public Guid? ApplicationId { get; set; }
    [Required, MaxLength(40)] public string ScholarshipType { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string CoverageType { get; set; } = "FIXED_AMOUNT";
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [Required, MaxLength(20)] public string DisbursementMethod { get; set; } = "FEE_CREDIT";
    [Required, MaxLength(20)] public string Status { get; set; } = "APPROVED";
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RenewalReviewAt { get; set; }
    public Guid? JournalEntryId { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(GrantId))] public FinanceGrant? Grant { get; set; }
}
