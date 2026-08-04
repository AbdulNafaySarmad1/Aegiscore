using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class ResearchLabRepository : Repository<ResearchLab>, IResearchLabRepository
{
    public ResearchLabRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ResearchLab>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(l => l.DepartmentId == departmentId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResearchLab>> GetBySupervisorAsync(Guid supervisorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(l => l.LabSupervisorId == supervisorId).ToListAsync(cancellationToken);
    }

    public async Task<ResearchLab?> GetWithEquipmentAsync(Guid labId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.Equipment)
            .FirstOrDefaultAsync(l => l.Id == labId, cancellationToken);
    }
}
