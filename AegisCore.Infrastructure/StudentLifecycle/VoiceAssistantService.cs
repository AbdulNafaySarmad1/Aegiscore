using System.Text.Json;
using AegisCore.Application.StudentLifecycle;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.StudentLifecycle;

public sealed class VoiceAssistantService(AegisDbContext db, ILogger<VoiceAssistantService> logger) : IVoiceAssistantService
{
    private static readonly Dictionary<string, (string Urdu, string English)> IntentPatterns = new()
    {
        ["QUERY_RESULTS"] = ("mera result", "my result"),
        ["QUERY_OUTSTANDING_FEES"] = ("fees kitni baki", "outstanding fees"),
        ["QUERY_TIMETABLE"] = ("schedule", "timetable"),
        ["QUERY_ATTENDANCE"] = ("attendance", "attendance"),
        ["REQUEST_DOCUMENT"] = ("roll number slip", "document request"),
        ["INITIATE_LEAVE"] = ("chutti", "leave"),
        ["SCHEDULE_APPOINTMENT"] = ("milna hai", "appointment")
    };

    public async Task<VoiceSessionDto> ProcessVoiceQueryAsync(VoiceInputDto input, CancellationToken ct = default)
    {
        var text = input.TextInput ?? "[audio input]";
        var detectedLanguage = DetectLanguage(text);
        var (intent, confidence) = ClassifyIntent(text);
        var responseText = intent switch
        {
            "QUERY_RESULTS" => "آپ کا GPA پچھلے سمسٹر میں 3.4 تھا۔ مزید تفصیل چاہیے؟",
            "QUERY_OUTSTANDING_FEES" => "آپ کی 25,000 روپے فیس باقی ہے۔ ادائیگی پلان کے بارے میں جانیں؟",
            "QUERY_TIMETABLE" => "آپ کا کل پہلا لیکچر صبح 9 بجے ہے۔",
            "QUERY_ATTENDANCE" => "آپ کی حاضری 85% ہے۔",
            _ => "میں آپ کی مدد کیسے کر سکتا ہوں؟"
        };

        var session = new VoiceSession
        {
            Id = Guid.NewGuid(), UserId = input.UserId != null ? Guid.Parse(input.UserId) : null,
            UserRole = input.UserRole, DetectedLanguage = detectedLanguage, Transcript = text,
            Intent = intent, EntitiesJson = "{}", ResponseText = responseText,
            AsrConfidence = confidence, TenantId = db.GetRequiredTenantId()
        };
        db.Set<VoiceSession>().Add(session);

        if (confidence < 0.5m) session.WasEscalated = true;
        await db.SaveChangesAsync(ct);

        return new VoiceSessionDto(session.Id, session.Transcript, session.DetectedLanguage, session.Intent, session.EntitiesJson, session.ResponseText, session.ResponseAudioUrl, session.AsrConfidence, session.WasEscalated, session.EscalationReason);
    }

    public async Task<IReadOnlyList<VoiceIntentDto>> GetActiveIntentsAsync(Guid tenantId, CancellationToken ct = default)
    {
        db.EnsureCurrentTenant(tenantId);
        var intents = await db.Set<VoiceIntent>().Where(i => i.IsActive).ToListAsync(ct);
        return intents.Select(i => new VoiceIntentDto(i.Id, i.IntentName, i.UrduTriggerPhrases, i.EnglishTriggerPhrases, i.PermittedRole)).ToList();
    }

    public async Task<EscalationLogDto> EscalateToHumanAsync(Guid sessionId, string reason, CancellationToken ct = default)
    {
        var session = await db.Set<VoiceSession>().SingleOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Voice session not found.");
        var log = new EscalationLog
        {
            Id = Guid.NewGuid(), VoiceSessionId = sessionId, EscalationType = "manual", ReasonDetail = reason,
            EscalatedAt = DateTimeOffset.UtcNow, TenantId = session.TenantId
        };
        db.Set<EscalationLog>().Add(log);
        await db.SaveChangesAsync(ct);
        return new EscalationLogDto(log.Id, log.EscalationType, log.ReasonDetail, log.EscalatedAt, log.AssignedTo);
    }

    private static string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "en";
        return text.Any(c => c >= 0x0600 && c <= 0x06FF) ? "ur" : "en";
    }

    private static (string Intent, decimal Confidence) ClassifyIntent(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return ("unknown", 0m);
        var lowered = text.ToLowerInvariant();
        foreach (var kvp in IntentPatterns)
        {
            if (string.Equals(kvp.Value.Urdu, lowered) || string.Equals(kvp.Value.English, lowered))
                return (kvp.Key, 0.9m);
            if (lowered.Contains(kvp.Value.English, StringComparison.OrdinalIgnoreCase))
                return (kvp.Key, 0.7m);
        }
        return ("unknown", 0.3m);
    }

}
