using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

public sealed class FinanceControlEvaluationBatch : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public int ControlCount { get; set; }
    public int PassedCount { get; set; }
    public int FlaggedCount { get; set; }
    public string ResultsJson { get; set; } = "[]";
    public string ContentHash { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FinanceSodRule : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IncompatibleActionsJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FinanceAuditorEngagement : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string AuditorName { get; set; } = string.Empty;
    public string AuditorEmail { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string ScopesJson { get; set; } = "[]";
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
}

public sealed class FinanceAuditSample : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AuditorEngagementId { get; set; }
    public string PopulationType { get; set; } = string.Empty;
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public int PopulationCount { get; set; }
    public int SampleSize { get; set; }
    public string SeedHash { get; set; } = string.Empty;
    public string SelectedReferencesJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class FinanceAuditEvidencePackage : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? AuditorEngagementId { get; set; }
    public string Framework { get; set; } = string.Empty;
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string PqcAlgorithm { get; set; } = string.Empty;
    public string PqcKeyId { get; set; } = string.Empty;
    public byte[] PqcSignature { get; set; } = [];
    public byte[] PqcPublicKey { get; set; } = [];
    public Guid GeneratedByUserId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
