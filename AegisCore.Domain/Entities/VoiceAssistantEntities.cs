using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("VoiceSessions")]
public class VoiceSession : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    [MaxLength(20)] public string UserRole { get; set; } = "student";
    [MaxLength(10)] public string DetectedLanguage { get; set; } = "en";
    [MaxLength(2000)] public string? Transcript { get; set; }
    [MaxLength(100)] public string? Intent { get; set; }
    [MaxLength(2000)] public string? EntitiesJson { get; set; }
    [MaxLength(2000)] public string? ResponseText { get; set; }
    [MaxLength(500)] public string? ResponseAudioUrl { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal? AsrConfidence { get; set; }
    public bool IsSlowMode { get; set; }
    public bool WasEscalated { get; set; }
    [MaxLength(20)] public string? EscalationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("VoiceIntents")]
public class VoiceIntent : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(100)] public string IntentName { get; set; } = string.Empty;
    [MaxLength(500)] public string? UrduTriggerPhrases { get; set; }
    [MaxLength(500)] public string? EnglishTriggerPhrases { get; set; }
    [MaxLength(100)] public string? ActionEndpoint { get; set; }
    [MaxLength(20)] public string PermittedRole { get; set; } = "student";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("EscalationLogs")]
public class EscalationLog : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid? VoiceSessionId { get; set; }
    [MaxLength(100)] public string EscalationType { get; set; } = "human_request";
    [MaxLength(500)] public string? ReasonDetail { get; set; }
    public DateTimeOffset EscalatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
    [MaxLength(100)] public string? AssignedTo { get; set; }
    public Guid TenantId { get; set; }
}
