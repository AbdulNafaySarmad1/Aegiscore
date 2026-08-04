namespace AegisCore.Application.AI;

public class AiAgentResponse
{
    public string AnswerText { get; set; } = string.Empty;
    public string? SuggestedAction { get; set; }
    public bool IsEscalated { get; set; }
    public double ResponseTimeMs { get; set; }
    public List<string> Sources { get; set; } = new();
    public object? StructuredData { get; set; }
}
