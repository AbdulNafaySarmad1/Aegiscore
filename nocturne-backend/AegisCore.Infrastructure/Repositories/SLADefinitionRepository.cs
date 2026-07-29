using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class SLADefinitionRepository : Repository<SLADefinition>, ISLADefinitionRepository
{
    public SLADefinitionRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<SLADefinition?> GetByCategoryAndPriorityAsync(MaintenanceCategory category, TicketPriority priority, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Category == category && s.Priority == priority && s.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<SLADefinition>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.IsActive).ToListAsync(cancellationToken);
    }
}
