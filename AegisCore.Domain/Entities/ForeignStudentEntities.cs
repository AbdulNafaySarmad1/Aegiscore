using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("ForeignStudentRecords")]
public class ForeignStudentRecord : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    [Required, MaxLength(100)] public string Nationality { get; set; } = string.Empty;
    [MaxLength(50)] public string PassportNumber { get; set; } = string.Empty;
    public DateTimeOffset PassportExpiry { get; set; }
    [MaxLength(50)] public string VisaType { get; set; } = "Student";
    [MaxLength(50)] public string? VisaNumber { get; set; }
    public DateTimeOffset VisaExpiry { get; set; }
    public DateTimeOffset EntryDate { get; set; }
    public int PermittedStayDays { get; set; }
    [MaxLength(20)] public string MoraRegistrationStatus { get; set; } = "pending";
    [MaxLength(20)] public string ComplianceStatus { get; set; } = "fully_compliant";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    public ICollection<DocumentExpiryTracker> DocumentTrackers { get; set; } = [];
}

[Table("DocumentExpiryTrackers")]
public class DocumentExpiryTracker : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ForeignStudentRecordId { get; set; }
    [Required, MaxLength(50)] public string DocumentType { get; set; } = string.Empty;
    public DateTimeOffset ExpiryDate { get; set; }
    public DateTimeOffset? Alert90DaysSent { get; set; }
    public DateTimeOffset? Alert60DaysSent { get; set; }
    public DateTimeOffset? Alert30DaysSent { get; set; }
    public DateTimeOffset? Alert14DaysSent { get; set; }
    public DateTimeOffset? Alert7DaysSent { get; set; }
    public bool IsExpired { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(ForeignStudentRecordId))] public ForeignStudentRecord ForeignStudentRecord { get; set; } = null!;
}

[Table("MoraSubmissions")]
public class MoraSubmission : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid SemesterId { get; set; }
    [MaxLength(50)] public string SubmissionType { get; set; } = "enrollment";
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public DateTimeOffset? SubmittedAt { get; set; }
    [MaxLength(50)] public string? MoraReferenceNumber { get; set; }
    [MaxLength(500)] public string? ResponseNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("EnrollmentLetters")]
public class EnrollmentLetter : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    [MaxLength(200)] public string? Purpose { get; set; }
    [MaxLength(500)] public string? PdfUrl { get; set; }
    [MaxLength(200)] public string? QrVerificationCode { get; set; }
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public Guid IssuedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("ComplianceBreaches")]
public class ComplianceBreach : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ForeignStudentRecordId { get; set; }
    [MaxLength(100)] public string BreachType { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
    [MaxLength(20)] public string? ResolutionType { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("VisaRenewalWorkflows")]
public class VisaRenewalWorkflow : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ForeignStudentRecordId { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "draft";
    public DateTimeOffset ApplicationDeadline { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    [MaxLength(20)] public string? RenewalOutcome { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
