using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Maintenance;

public class WorkOrderService
{
    private readonly IWorkOrderRepository _workOrderRepository;
    private readonly IMaintenanceTicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkOrderService(
        IWorkOrderRepository workOrderRepository,
        IMaintenanceTicketRepository ticketRepository,
        IUnitOfWork unitOfWork)
    {
        _workOrderRepository = workOrderRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkOrder> CreateWorkOrderAsync(Guid ticketId, WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new InvalidOperationException("Ticket not found.");

        workOrder.MaintenanceTicketId = ticketId;
        workOrder.WorkOrderNumber = $"WO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        workOrder.Status = WorkOrderStatus.Assigned;

        ticket.Status = TicketStatus.InProgress;

        var created = await _workOrderRepository.AddAsync(workOrder, cancellationToken);
        await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task StartWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Work order not found.");

        workOrder.Status = WorkOrderStatus.InProgress;
        workOrder.StartedAt = DateTime.UtcNow;
        workOrder.UpdatedAt = DateTime.UtcNow;

        await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteWorkOrderAsync(Guid workOrderId, string resolutionNotes, CancellationToken cancellationToken = default)
    {
        var workOrder = await _workOrderRepository.GetWithDetailsAsync(workOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Work order not found.");

        workOrder.Status = WorkOrderStatus.Completed;
        workOrder.CompletedAt = DateTime.UtcNow;
        workOrder.ResolutionNotes = resolutionNotes;
        workOrder.TotalCost = workOrder.LaborCost + workOrder.MaterialCost;
        workOrder.UpdatedAt = DateTime.UtcNow;

        await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SignOffWorkOrderAsync(Guid workOrderId, Guid signedOffByUserId, CancellationToken cancellationToken = default)
    {
        var workOrder = await _workOrderRepository.GetByIdAsync(workOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Work order not found.");

        if (workOrder.Status != WorkOrderStatus.Completed)
            throw new InvalidOperationException("Only completed work orders can be signed off.");

        workOrder.Status = WorkOrderStatus.SignedOff;
        workOrder.SignedOffAt = DateTime.UtcNow;
        workOrder.SignedOffByUserId = signedOffByUserId;
        workOrder.UpdatedAt = DateTime.UtcNow;

        await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<WorkOrderTimeEntry> AddTimeEntryAsync(Guid workOrderId, WorkOrderTimeEntry timeEntry, CancellationToken cancellationToken = default)
    {
        var workOrder = await _workOrderRepository.GetWithDetailsAsync(workOrderId, cancellationToken)
            ?? throw new InvalidOperationException("Work order not found.");

        timeEntry.WorkOrderId = workOrderId;
        workOrder.TimeEntries.Add(timeEntry);
        workOrder.HoursWorked += timeEntry.HoursWorked;

        await _workOrderRepository.UpdateAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return timeEntry;
    }
}
