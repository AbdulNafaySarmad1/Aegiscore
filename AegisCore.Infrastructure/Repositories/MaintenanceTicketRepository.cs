using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class MaintenanceTicketRepository : Repository<MaintenanceTicket>, IMaintenanceTicketRepository
{
    public MaintenanceTicketRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<MaintenanceTicket>> GetByStatusAsync(TicketStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceTicket>> GetByCategoryAsync(MaintenanceCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.Category == category).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceTicket>> GetByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.AssetId == assetId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceTicket>> GetByCreatedUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.CreatedByUserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceTicket>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.SLADeadline.HasValue && t.SLADeadline < DateTime.UtcNow && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceTicket>> GetSLABreachedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(t => t.IsSLABreached).ToListAsync(cancellationToken);
    }

    public async Task<MaintenanceTicket?> GetWithDetailsAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.WorkOrders)
            .Include(t => t.Comments)
            .Include(t => t.Escalations)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
    }
}
