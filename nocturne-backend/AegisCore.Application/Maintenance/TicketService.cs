using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Maintenance;

public class TicketService
{
    private readonly IMaintenanceTicketRepository _ticketRepository;
    private readonly ISLADefinitionRepository _slaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TicketService(
        IMaintenanceTicketRepository ticketRepository,
        ISLADefinitionRepository slaRepository,
        IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _slaRepository = slaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MaintenanceTicket> CreateTicketAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        ticket.TicketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        ticket.Status = TicketStatus.Open;

        var sla = await _slaRepository.GetByCategoryAndPriorityAsync(ticket.Category, ticket.Priority, cancellationToken);
        if (sla != null)
        {
            ticket.SLADeadline = DateTime.UtcNow.AddHours(sla.ResolutionTimeHours);
        }

        var created = await _ticketRepository.AddAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task AssignTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new InvalidOperationException("Ticket not found.");

        if (ticket.Status != TicketStatus.Open)
            throw new InvalidOperationException("Only open tickets can be assigned.");

        ticket.Status = TicketStatus.Assigned;
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new InvalidOperationException("Ticket not found.");

        ticket.Status = TicketStatus.Resolved;
        ticket.ClosedAt = DateTime.UtcNow;

        if (ticket.SLADeadline.HasValue && DateTime.UtcNow > ticket.SLADeadline.Value)
        {
            ticket.IsSLABreached = true;
        }

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new InvalidOperationException("Ticket not found.");

        if (ticket.Status != TicketStatus.Resolved)
            throw new InvalidOperationException("Only resolved tickets can be closed.");

        ticket.Status = TicketStatus.Closed;
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<TicketComment> AddCommentAsync(Guid ticketId, TicketComment comment, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new InvalidOperationException("Ticket not found.");

        comment.MaintenanceTicketId = ticketId;
        ticket.Comments.Add(comment);

        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task<TicketDashboardDto> GetTicketDashboardAsync(CancellationToken cancellationToken = default)
    {
        var openTickets = await _ticketRepository.GetByStatusAsync(TicketStatus.Open, cancellationToken);
        var inProgressTickets = await _ticketRepository.GetByStatusAsync(TicketStatus.InProgress, cancellationToken);
        var overdueTickets = await _ticketRepository.GetOverdueAsync(cancellationToken);
        var slaBreachedTickets = await _ticketRepository.GetSLABreachedAsync(cancellationToken);

        return new TicketDashboardDto
        {
            OpenTickets = openTickets.Count,
            InProgressTickets = inProgressTickets.Count,
            OverdueTickets = overdueTickets.Count,
            SLABreachedTickets = slaBreachedTickets.Count,
            AverageResolutionTimeHours = 0
        };
    }
}

public class TicketDashboardDto
{
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int OverdueTickets { get; set; }
    public int SLABreachedTickets { get; set; }
    public double AverageResolutionTimeHours { get; set; }
}
