using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class WorkOrderRepository : Repository<WorkOrder>, IWorkOrderRepository
{
    public WorkOrderRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<WorkOrder>> GetByTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(w => w.MaintenanceTicketId == ticketId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrder>> GetByTechnicianAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(w => w.AssignedToEmployeeId == employeeId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrder>> GetByStatusAsync(WorkOrderStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(w => w.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrder>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(w => w.Status == WorkOrderStatus.Assigned || w.Status == WorkOrderStatus.InProgress).ToListAsync(cancellationToken);
    }

    public async Task<WorkOrder?> GetWithDetailsAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(w => w.Materials)
            .Include(w => w.TimeEntries)
            .FirstOrDefaultAsync(w => w.Id == workOrderId, cancellationToken);
    }
}
