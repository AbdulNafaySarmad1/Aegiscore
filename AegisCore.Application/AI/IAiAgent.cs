namespace AegisCore.Application.AI;

public interface IAiAgent
{
    string AgentName { get; }
    string Domain { get; }
    Task<AiAgentResponse> ExecuteAsync(AiAgentRequest request, CancellationToken cancellationToken = default);
}
