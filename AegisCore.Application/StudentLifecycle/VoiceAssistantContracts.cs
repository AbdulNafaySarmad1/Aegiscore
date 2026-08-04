namespace AegisCore.Application.StudentLifecycle;

public interface IVoiceAssistantService
{
    Task<VoiceSessionDto> ProcessVoiceQueryAsync(VoiceInputDto input, CancellationToken ct = default);
    Task<IReadOnlyList<VoiceIntentDto>> GetActiveIntentsAsync(Guid tenantId, CancellationToken ct = default);
    Task<EscalationLogDto> EscalateToHumanAsync(Guid sessionId, string reason, CancellationToken ct = default);
}

public sealed record VoiceInputDto(
    string? AudioBase64,
    string? TextInput,
    string? UserId,
    string UserRole);

public sealed record VoiceSessionDto(
    Guid Id,
    string? Transcript,
    string DetectedLanguage,
    string? Intent,
    string? EntitiesJson,
    string? ResponseText,
    string? ResponseAudioUrl,
    decimal? AsrConfidence,
    bool WasEscalated,
    string? EscalationReason);

public sealed record VoiceIntentDto(
    Guid Id,
    string IntentName,
    string? UrduTriggerPhrases,
    string? EnglishTriggerPhrases,
    string PermittedRole);

public sealed record EscalationLogDto(
    Guid Id,
    string EscalationType,
    string? ReasonDetail,
    DateTimeOffset EscalatedAt,
    string? AssignedTo);
