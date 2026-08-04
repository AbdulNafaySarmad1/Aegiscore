using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class CampusRepository : Repository<CampusEntity>, ICampusRepository
{
    public CampusRepository(AegisDbContext context) : base(context) { }

    public async Task<CampusEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.CampusCode == code, cancellationToken);
    }

    public async Task<IReadOnlyList<CampusEntity>> GetByStatusAsync(CampusStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CampusEntity>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(c => c.TenantId == tenantId).ToListAsync(cancellationToken);
    }
}

public class BuildingRepository : Repository<Building>, IBuildingRepository
{
    public BuildingRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Building>> GetByCampusAsync(Guid campusId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(b => b.CampusId == campusId).ToListAsync(cancellationToken);
    }
}

public class FloorRepository : Repository<Floor>, IFloorRepository
{
    public FloorRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Floor>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(f => f.BuildingId == buildingId).ToListAsync(cancellationToken);
    }
}

public class RoomRepository : Repository<Room>, IRoomRepository
{
    public RoomRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Room>> GetByFloorAsync(Guid floorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.FloorId == floorId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetByCampusAsync(Guid campusId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.Floor.Building.CampusId == campusId).ToListAsync(cancellationToken);
    }
}

public class CampusConfigurationRepository : Repository<CampusConfiguration>, ICampusConfigurationRepository
{
    public CampusConfigurationRepository(AegisDbContext context) : base(context) { }

    public async Task<CampusConfiguration?> GetByCampusAsync(Guid campusId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.CampusId == campusId, cancellationToken);
    }
}
