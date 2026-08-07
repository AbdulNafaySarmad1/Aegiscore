using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// Cross-cutting platform entities preserved from finance-onboarding-foundation

// Cross-cutting: Personality Mode
[Table("PersonalityConfigs")] public class PersonalityConfig { [Key] public Guid Id { get; set; }
    public bool IsEnabled { get; set; } public bool IsLockedOff { get; set; }
    public Guid? UserId { get; set; } public Guid? RoleId { get; set; }
    public bool IsActiveForUser { get; set; } public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// Cross-cutting: Chart System
[Table("ChartConfigurations")] public class ChartConfiguration { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public ChartType Type { get; set; }
    [MaxLength(500)] public string? DataSourceKey { get; set; } [Column(TypeName = "jsonb")] public string ConfigJson { get; set; } = "{}";
    [MaxLength(200)] public string? InsightI18nKey { get; set; } public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("InsightChips")] public class InsightChip { [Key] public Guid Id { get; set; }
    public Guid ChartConfigurationId { get; set; } public InsightType Type { get; set; }
    [MaxLength(500)] public string Text { get; set; } = ""; [MaxLength(200)] public string? I18nKey { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

// Cross-cutting: OCR Integration
[Table("OcrProviderConfigs")] public class OcrProviderConfig { [Key] public Guid Id { get; set; }
    [MaxLength(50)] public string Provider { get; set; } = "tesseract"; [MaxLength(500)] public string? ApiEndpoint { get; set; }
    [MaxLength(500)] public string? CredentialRef { get; set; } public bool IsActive { get; set; }
    public int Priority { get; set; } = 1; public Guid TenantId { get; set; } }
