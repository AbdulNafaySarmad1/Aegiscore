namespace AegisCore.Application.StudentLifecycle;

public interface IMentalHealthService
{
    Task<MentalHealthTicketDto> CreateTicketAsync(AnonymousTicketRequest request, CancellationToken ct = default);
    Task<CounselorAssignmentDto> RouteTicketToCounselorAsync(Guid ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<MentalHealthTicketDto>> GetCounselorQueueAsync(Guid counselorId, CancellationToken ct = default);
    Task CloseTicketAsync(Guid ticketId, string resolutionType, CancellationToken ct = default);
    Task CheckForCrisisFlagsAsync(Guid ticketId, CancellationToken ct = default);
    Task<AggregateStatsDto> GetAggregateStatsAsync(Guid tenantId, CancellationToken ct = default);
}

public sealed record AnonymousTicketRequest(
    string ConcernDescription,
    string ConcernType,
    string Severity,
    string? PreferredLanguage);

public sealed record MentalHealthTicketDto(
    Guid Id,
    string AnonymousToken,
    string Severity,
    string ConcernType,
    string? ConcernDescription,
    string Status,
    Guid? AssignedCounselorId,
    DateTimeOffset? FirstResponseAt,
    bool CrisisProtocolsTriggered,
    bool MandatoryReportTriggered);

public sealed record CounselorAssignmentDto(
    Guid TicketId,
    Guid CounselorId,
    string CounselorName,
    string AssignmentRationale,
    int ExpectedResponseHours);

public sealed record AggregateStatsDto(
    int TotalTicketsThisMonth,
    int OpenTickets,
    int ResolvedTickets,
    Dictionary<string, int> TicketsByConcernType,
    Dictionary<string, int> TicketsBySeverity,
    decimal AverageResponseTimeHours,
    int CrisisEscalationsThisMonth,
    List<string> PeakStressPeriods);
