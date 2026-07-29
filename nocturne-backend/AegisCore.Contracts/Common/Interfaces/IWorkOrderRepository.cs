using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    Task<IReadOnlyList<WorkOrder>> GetByTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrder>> GetByTechnicianAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrder>> GetByStatusAsync(WorkOrderStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrder>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetWithDetailsAsync(Guid workOrderId, CancellationToken cancellationToken = default);
}
