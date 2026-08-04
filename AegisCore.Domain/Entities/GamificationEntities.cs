using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("BadgeDefinitions")]
public class BadgeDefinition : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(200)] public string BadgeName { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(50)] public string Category { get; set; } = "academic";
    [MaxLength(500)] public string? CriteriaJson { get; set; }
    [MaxLength(500)] public string? BadgeImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("BadgeAwards")]
public class BadgeAward : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid BadgeDefinitionId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? SemesterId { get; set; }
    [MaxLength(500)] public string? AchievementDetail { get; set; }
    [MaxLength(200)] public string? VerificationHash { get; set; }
    public DateTimeOffset AwardedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool ParentNotified { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("DeansListEntries")]
public class DeansListEntry : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    [Column(TypeName = "decimal(4,2)")] public decimal Cgpa { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending_verification";
    public bool ParentNotified { get; set; }
    [MaxLength(500)] public string? FormalLetterUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("LeaderboardEntries")]
public class LeaderboardEntry : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    [MaxLength(20)] public string Scope { get; set; } = "class";
    [MaxLength(50)] public string Metric { get; set; } = "cgpa";
    [Column(TypeName = "decimal(4,2)")] public decimal Score { get; set; }
    public int Rank { get; set; }
    public bool IsAnonymized { get; set; }
    public bool IsOptedOut { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("AchievementRoadmaps")]
public class AchievementRoadmap : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid? BadgeDefinitionId { get; set; }
    [MaxLength(200)] public string? NextMilestone { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal ProgressPercent { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
