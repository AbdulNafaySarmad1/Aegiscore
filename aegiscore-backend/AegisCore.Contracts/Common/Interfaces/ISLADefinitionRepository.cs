using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Contracts.Common.Interfaces;

public interface ISLADefinitionRepository : IRepository<SLADefinition>
{
    Task<SLADefinition?> GetByCategoryAndPriorityAsync(MaintenanceCategory category, TicketPriority priority, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SLADefinition>> GetActiveAsync(CancellationToken cancellationToken = default);
}
