using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class LabEquipmentRepository : Repository<LabEquipment>, ILabEquipmentRepository
{
    public LabEquipmentRepository(AegisDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LabEquipment>> GetByLabAsync(Guid labId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.ResearchLabId == labId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LabEquipment>> GetByStatusAsync(EquipmentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<LabEquipment?> GetByAssetTagAsync(string assetTag, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.AssetTag == assetTag, cancellationToken);
    }
}
