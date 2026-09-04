using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("UniversitySsoProfiles")]
public class UniversitySsoProfile : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Domain { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string SsoProvider { get; set; } = "aegiscore";
    public string? SsoTenantId { get; set; }
    public string? SsoClientId { get; set; }
    public string? SsoClientSecretEncrypted { get; set; }
    public bool Require2Fa { get; set; } = true;
    public bool RequirePasswordChangeOnFirstLogin { get; set; } = true;
    public string? LogoUrl { get; set; }
    [MaxLength(7)] public string? PrimaryColor { get; set; }
    [MaxLength(7)] public string? SecondaryColor { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("UserSecondFactors")]
public class UserSecondFactor : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid UserId { get; set; }
    [Required, MaxLength(20)] public string Method { get; set; } = "authenticator";
    [Required] public string SecretEncrypted { get; set; } = string.Empty;
    [Required] public string BackupCodesHashCsv { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("SsoSessions")]
public class SsoSession : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid UserId { get; set; }
    [Required, MaxLength(20)] public string Provider { get; set; } = "aegiscore";
    [Required] public string AccessTokenEncrypted { get; set; } = string.Empty;
    public string? RefreshTokenEncrypted { get; set; }
    public DateTimeOffset TokenExpiresAt { get; set; }
    [MaxLength(255)] public string? DeviceId { get; set; }
    [MaxLength(200)] public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
}

[Table("ExamPaperBatches")]
public class ExamPaperBatch : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public int PaperCount { get; set; }
    [Required, MaxLength(40)] public string Status { get; set; } = "prepared";
    public Guid PreparedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("ExamPaperScans")]
public class ExamPaperScan : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid StudentId { get; set; }
    [Required, MaxLength(120)] public string QrPayload { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public bool BlurDetected { get; set; }
    public bool BlankDetected { get; set; }
    [Required] public string OcrText { get; set; } = string.Empty;
    public DateTimeOffset ScannedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("AgenticGradeRuns")]
public class AgenticGradeRun : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid ScanId { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal SuggestedMarks { get; set; }
    [Column(TypeName = "decimal(4,3)")] public decimal Confidence { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "review_required";
    [Required] public string AgentTraceJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("HumanGradeReviews")]
public class HumanGradeReview : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid GradeRunId { get; set; }
    public Guid? ReviewedBy { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? FinalMarks { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "pending";
    [MaxLength(1000)] public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
}

[Table("PaperDestructionManifests")]
public class PaperDestructionManifest : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key] public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public int PaperCount { get; set; }
    [Required, MaxLength(64)] public string ManifestHash { get; set; } = string.Empty;
    public Guid WitnessedBy { get; set; }
    public DateTimeOffset DestroyedAt { get; set; } = DateTimeOffset.UtcNow;
}
