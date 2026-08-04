using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("DefaultRiskAssessments")]
public class DefaultRiskAssessment : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal DefaultProbabilityScore { get; set; }
    [Required, MaxLength(20)] public string RiskTier { get; set; } = "GREEN";
    [MaxLength(500)] public string? KeyRiskDrivers { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal PredictionConfidence { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal OutstandingAmount { get; set; }
    public int AverageDaysLate { get; set; }
    public int MaxDaysLate { get; set; }
    public int PartialPaymentCount { get; set; }
    public int BouncedPaymentCount { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal PaymentTrendScore { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal ScholarshipCoveragePercent { get; set; }
    public bool IsHostelStudent { get; set; }
    public DateTimeOffset AssessedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("PaymentPlanArrangements")]
public class PaymentPlanArrangement : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal InstallmentAmount { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "active";
    public DateTimeOffset FirstInstallmentDate { get; set; }
    public DateTimeOffset LastInstallmentDate { get; set; }
    public Guid ApprovedBy { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    public ICollection<PaymentPlanInstallment> Installments { get; set; } = [];
}

[Table("PaymentPlanInstallments")]
public class PaymentPlanInstallment : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid PaymentPlanArrangementId { get; set; }
    public int InstallmentNumber { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Amount { get; set; }
    public DateTimeOffset DueDate { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal PaidAmount { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(PaymentPlanArrangementId))] public PaymentPlanArrangement PaymentPlanArrangement { get; set; } = null!;
}

[Table("InterventionRecords")]
public class InterventionRecord : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    [Required, MaxLength(20)] public string InterventionType { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string RiskTier { get; set; } = "GREEN";
    [MaxLength(2000)] public string? InterventionDetails { get; set; }
    public Guid InitiatedBy { get; set; }
    public DateTimeOffset InitiatedAt { get; set; } = DateTimeOffset.UtcNow;
    [Required, MaxLength(20)] public string Outcome { get; set; } = "pending";
    [MaxLength(1000)] public string? OutcomeNotes { get; set; }
    public DateTimeOffset? OutcomeAt { get; set; }
    public bool WasSuccessful { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("CashFlowForecasts")]
public class CashFlowForecast : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid SemesterId { get; set; }
    public Guid AcademicYearId { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal ExpectedCollection { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal ProjectedShortfall { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal RecoveryRateEstimate { get; set; }
    public int ForecastWeek { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal WeeklyExpectedInflow { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal WeeklyActualInflow { get; set; }
    public DateTimeOffset ForecastDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("EthicalAuditLogs")]
public class EthicalAuditLog : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid? RelatedAssessmentId { get; set; }
    [Required, MaxLength(100)] public string AuditCategory { get; set; } = string.Empty;
    [MaxLength(500)] public string? AuditDetail { get; set; }
    public bool IsViolation { get; set; }
    public DateTimeOffset LoggedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
