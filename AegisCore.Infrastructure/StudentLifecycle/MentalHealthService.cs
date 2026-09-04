using System.Text.Json;
using AegisCore.Application.StudentLifecycle;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.StudentLifecycle;

public sealed class MentalHealthService(AegisDbContext db, ILogger<MentalHealthService> logger) : IMentalHealthService
{
    private static readonly string[] CrisisKeywords = ["want to die", "hurt myself", "kill myself", "no reason to live", "disappear", "nobody would miss me"];

    public async Task<MentalHealthTicketDto> CreateTicketAsync(AnonymousTicketRequest request, CancellationToken ct = default)
    {
        var token = Guid.NewGuid().ToString("N")[..12];
        var isCrisis = CrisisKeywords.Any(k => (request.ConcernDescription ?? "").Contains(k, StringComparison.OrdinalIgnoreCase));
        var ticket = new MentalHealthTicket
        {
            Id = Guid.NewGuid(), AnonymousToken = token, Severity = isCrisis ? "crisis" : request.Severity,
            ConcernType = request.ConcernType, ConcernDescription = request.ConcernDescription,
            PreferredLanguage = request.PreferredLanguage ?? "en",
            Status = "open", CrisisProtocolsTriggered = isCrisis, TenantId = await GetTenantIdAsync(ct)
        };
        db.Set<MentalHealthTicket>().Add(ticket);
        await db.SaveChangesAsync(ct);
        return MapToDto(ticket);
    }

    public async Task<CounselorAssignmentDto> RouteTicketToCounselorAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await db.Set<MentalHealthTicket>().FindAsync([ticketId], ct) ?? throw new InvalidOperationException("Ticket not found.");
        var counselors = await db.Set<CounselorProfile>().Where(c => c.IsActive && c.CurrentCaseload < c.MaxCaseload).OrderBy(c => c.CurrentCaseload).ToListAsync(ct);
        var assigned = counselors.FirstOrDefault() ?? throw new InvalidOperationException("No available counselors.");
        ticket.AssignedCounselorId = assigned.Id;
        ticket.AssignedAt = DateTimeOffset.UtcNow;
        ticket.Status = "assigned";
        assigned.CurrentCaseload++;
        await db.SaveChangesAsync(ct);
        return new CounselorAssignmentDto(ticket.Id, assigned.Id, assigned.FullName, $"Assigning to {assigned.FullName} based on availability and caseload.", 24);
    }

    public async Task<IReadOnlyList<MentalHealthTicketDto>> GetCounselorQueueAsync(Guid counselorId, CancellationToken ct = default)
    {
        var tickets = await db.Set<MentalHealthTicket>().Where(t => t.AssignedCounselorId == counselorId && t.Status != "resolved").OrderByDescending(t => t.Severity == "crisis" ? 3 : t.Severity == "urgent" ? 2 : 1).ThenBy(t => t.CreatedAt).ToListAsync(ct);
        return tickets.Select(MapToDto).ToList();
    }

    public async Task CloseTicketAsync(Guid ticketId, string resolutionType, CancellationToken ct = default)
    {
        var ticket = await db.Set<MentalHealthTicket>().FindAsync([ticketId], ct) ?? throw new InvalidOperationException("Ticket not found.");
        ticket.Status = "resolved";
        ticket.ResolutionType = resolutionType;
        ticket.ResolvedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task CheckForCrisisFlagsAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await db.Set<MentalHealthTicket>().FindAsync([ticketId], ct) ?? throw new InvalidOperationException("Ticket not found.");
        var hasCrisis = CrisisKeywords.Any(k => (ticket.ConcernDescription ?? "").Contains(k, StringComparison.OrdinalIgnoreCase));
        if (hasCrisis && !ticket.CrisisProtocolsTriggered)
        {
            ticket.CrisisProtocolsTriggered = true;
            ticket.Severity = "crisis";
            var report = new MandatoryReportLog { Id = Guid.NewGuid(), TicketId = ticketId, TriggerCriterion = "crisis_keywords_detected", ActionTaken = "Immediate escalation to on-call counselor", ReportedTo = "Dean of Student Affairs", ReportedAt = DateTimeOffset.UtcNow, TenantId = ticket.TenantId };
            db.Set<MandatoryReportLog>().Add(report);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<AggregateStatsDto> GetAggregateStatsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tickets = await db.Set<MentalHealthTicket>().Where(t => t.TenantId == tenantId).ToListAsync(ct);
        var thisMonth = tickets.Where(t => t.CreatedAt >= DateTimeOffset.UtcNow.AddMonths(-1)).ToList();
        return new AggregateStatsDto(
            thisMonth.Count, tickets.Count(t => t.Status != "resolved"), tickets.Count(t => t.Status == "resolved"),
            thisMonth.GroupBy(t => t.ConcernType).ToDictionary(g => g.Key, g => g.Count()),
            thisMonth.GroupBy(t => t.Severity).ToDictionary(g => g.Key, g => g.Count()),
            (decimal)tickets.Where(t => t.FirstResponseAt.HasValue).Select(t => (t.FirstResponseAt!.Value - t.CreatedAt).TotalHours).DefaultIfEmpty(0).Average(),
            thisMonth.Count(t => t.CrisisProtocolsTriggered),
            ["Midterms Week", "Finals Week"]);
    }

    private static MentalHealthTicketDto MapToDto(MentalHealthTicket t) => new(t.Id, t.AnonymousToken, t.Severity, t.ConcernType, t.ConcernDescription, t.Status, t.AssignedCounselorId, t.FirstResponseAt, t.CrisisProtocolsTriggered, t.MandatoryReportTriggered);

    private async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        var first = await db.Set<MentalHealthTicket>().Select(t => t.TenantId).FirstOrDefaultAsync(ct);
        return first != Guid.Empty ? first : Guid.NewGuid();
    }
}
