using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IMaintenanceTicketRepository : IRepository<MaintenanceTicket>
{
    Task<IReadOnlyList<MaintenanceTicket>> GetByStatusAsync(TicketStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTicket>> GetByCategoryAsync(MaintenanceCategory category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTicket>> GetByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTicket>> GetByCreatedUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTicket>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceTicket>> GetSLABreachedAsync(CancellationToken cancellationToken = default);
    Task<MaintenanceTicket?> GetWithDetailsAsync(Guid ticketId, CancellationToken cancellationToken = default);
}
