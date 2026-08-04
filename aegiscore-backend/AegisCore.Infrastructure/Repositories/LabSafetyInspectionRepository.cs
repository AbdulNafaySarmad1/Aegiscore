using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class LabSafetyInspectionRepository : Repository<LabSafetyInspection>, ILabSafetyInspectionRepository
{
    public LabSafetyInspectionRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LabSafetyInspection>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.ResearchLabId == labId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LabSafetyInspection>> GetByStatusAsync(InspectionStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<LabSafetyInspection?> GetWithChecklistAsync(Guid inspectionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.ChecklistItems)
            .FirstOrDefaultAsync(i => i.Id == inspectionId, cancellationToken);
    }
}
