namespace AegisCore.Application.AI;

public class LlmOptions
{
    public int MaxTokens { get; set; } = 1024;
    public float Temperature { get; set; } = 0.7f;
    public float TopP { get; set; } = 1.0f;
    public float FrequencyPenalty { get; set; } = 0.0f;
    public float PresencePenalty { get; set; } = 0.0f;
    public List<string>? StopSequences { get; set; }
}
