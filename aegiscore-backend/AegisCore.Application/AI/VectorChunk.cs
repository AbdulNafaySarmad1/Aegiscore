namespace AegisCore.Application.AI;

public class VectorChunk
{
    public string Id { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, object?> Metadata { get; set; } = new();
    public float Score { get; set; }
}
