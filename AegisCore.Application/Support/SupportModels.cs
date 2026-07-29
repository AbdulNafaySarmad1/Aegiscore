namespace AegisCore.Application.Support;

public static class SupportTicketStatuses
{
    public const string Open = "open";
    public const string AiResolved = "ai_resolved";
    public const string Escalated = "escalated";
    public const string Closed = "closed";
}

public sealed record CreateSupportTicketRequest(
    string Title,
    string Description,
    string Category = "other",
    string Priority = "normal");

public sealed record SupportTicketDto(
    Guid Id,
    Guid TenantId,
    Guid RequesterId,
    string Title,
    string Description,
    string Category,
    string Priority,
    string Status,
    string? AiResponse,
    decimal? AiConfidence,
    int? CustomerRating,
    string? CustomerFeedback,
    string? HumanResponse,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

public sealed record SupportFeedbackRequest(int Rating, string? Feedback);

public sealed record ResolveEscalationRequest(string Response, bool AddToKnowledgeBase = true);

public sealed record KnowledgeArticleRequest(
    string Category,
    string Question,
    string Answer);

public sealed record SupportEscalationDto(
    Guid Id,
    Guid TicketId,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt,
    SupportTicketDto Ticket);

public sealed record SupportAnalytics(
    int TotalTickets,
    decimal AverageCsat,
    decimal AiResolutionRate,
    decimal EscalationRate,
    decimal AverageResolutionHours);

