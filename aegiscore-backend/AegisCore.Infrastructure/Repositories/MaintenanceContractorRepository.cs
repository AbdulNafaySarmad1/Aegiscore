using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class MaintenanceContractorRepository : Repository<MaintenanceContractor>, IMaintenanceContractorRepository
{
    public MaintenanceContractorRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<MaintenanceContractor>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.IsActive).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaintenanceContractor>> GetBySpecializationAsync(string specialization, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.Specialization.Contains(specialization)).ToListAsync(cancellationToken);
    }
}
