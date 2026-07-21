namespace AegisCore.Application.AI;

public class AiAgentRequest
{
    public string UserMessage { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid TenantId { get; set; }
    public string? UserRole { get; set; }
    public Dictionary<string, object?>? Context { get; set; }
    public AiAgentRequestMode Mode { get; set; } = AiAgentRequestMode.Chat;
}

public enum AiAgentRequestMode
{
    Chat,
    Analysis,
    Prediction,
    Recommendation,
    Extraction
}
