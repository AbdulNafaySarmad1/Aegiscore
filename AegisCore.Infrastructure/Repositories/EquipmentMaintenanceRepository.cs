using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class EquipmentMaintenanceRepository : Repository<EquipmentMaintenance>, IEquipmentMaintenanceRepository
{
    public EquipmentMaintenanceRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<EquipmentMaintenance>> GetByEquipmentAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(m => m.LabEquipmentId == equipmentId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EquipmentMaintenance>> GetByStatusAsync(MaintenanceStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(m => m.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EquipmentMaintenance>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(m => m.Status == MaintenanceStatus.Reported || m.Status == MaintenanceStatus.Assigned).ToListAsync(cancellationToken);
    }
}
