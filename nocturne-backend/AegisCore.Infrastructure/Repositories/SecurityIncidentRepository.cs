using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class SecurityIncidentRepository : Repository<SecurityIncident>, ISecurityIncidentRepository
{
    public SecurityIncidentRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SecurityIncident>> GetByStatusAsync(IncidentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityIncident>> GetByTypeAsync(IncidentType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.Type == type).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityIncident>> GetByReportedUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.ReportedByUserId == userId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityIncident>> GetByAssignedToAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(i => i.AssignedToEmployeeId == employeeId).ToListAsync(cancellationToken);
    }

    public async Task<SecurityIncident?> GetWithDetailsAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Witnesses)
            .Include(i => i.Evidences)
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);
    }
}
