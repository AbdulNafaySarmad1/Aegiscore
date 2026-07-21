using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Maintenance;

public class SLAEngineService
{
    private readonly ISLADefinitionRepository _slaRepository;
    private readonly IMaintenanceTicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SLAEngineService(
        ISLADefinitionRepository slaRepository,
        IMaintenanceTicketRepository ticketRepository,
        IUnitOfWork unitOfWork)
    {
        _slaRepository = slaRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SLADefinition> CreateSLADefinitionAsync(SLADefinition sla, CancellationToken cancellationToken = default)
    {
        var existing = await _slaRepository.GetByCategoryAndPriorityAsync(sla.Category, sla.Priority, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"SLA definition already exists for {sla.Category} - {sla.Priority}.");

        var created = await _slaRepository.AddAsync(sla, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task CheckSLAComplianceAsync(CancellationToken cancellationToken = default)
    {
        var openTickets = await _ticketRepository.GetByStatusAsync(TicketStatus.Open, cancellationToken);
        var assignedTickets = await _ticketRepository.GetByStatusAsync(TicketStatus.Assigned, cancellationToken);
        var inProgressTickets = await _ticketRepository.GetByStatusAsync(TicketStatus.InProgress, cancellationToken);

        var allActiveTickets = openTickets.Concat(assignedTickets).Concat(inProgressTickets).ToList();

        foreach (var ticket in allActiveTickets)
        {
            if (ticket.SLADeadline.HasValue && DateTime.UtcNow > ticket.SLADeadline.Value && !ticket.IsSLABreached)
            {
                ticket.IsSLABreached = true;
                await _ticketRepository.UpdateAsync(ticket, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<SLAReportDto> GetSLAReportAsync(CancellationToken cancellationToken = default)
    {
        var slaBreachedTickets = await _ticketRepository.GetSLABreachedAsync(cancellationToken);
        var allTickets = await _ticketRepository.GetAllAsync(cancellationToken);
        var closedTickets = allTickets.Where(t => t.Status == TicketStatus.Closed).ToList();

        return new SLAReportDto
        {
            TotalTickets = allTickets.Count,
            SLABreachedTickets = slaBreachedTickets.Count,
            SLAComplianceRate = allTickets.Count > 0
                ? (decimal)(allTickets.Count - slaBreachedTickets.Count) / allTickets.Count * 100
                : 100,
            AverageResolutionTimeHours = closedTickets.Any()
                ? closedTickets.Average(t => (t.ClosedAt!.Value - t.CreatedAt).TotalHours)
                : 0
        };
    }
}

public class SLAReportDto
{
    public int TotalTickets { get; set; }
    public int SLABreachedTickets { get; set; }
    public decimal SLAComplianceRate { get; set; }
    public double AverageResolutionTimeHours { get; set; }
}
