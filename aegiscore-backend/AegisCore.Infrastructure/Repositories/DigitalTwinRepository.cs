using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class IoTSensorRepository : Repository<IoTSensor>, IIoTSensorRepository
{
    public IoTSensorRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<IoTSensor>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.BuildingId == buildingId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IoTSensor>> GetByTypeAsync(SensorType type, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.SensorType == type).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IoTSensor>> GetByStatusAsync(SensorStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.Status == status).ToListAsync(cancellationToken);
    }
}

public class SensorReadingRepository : Repository<SensorReading>, ISensorReadingRepository
{
    public SensorReadingRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<SensorReading>> GetBySensorAsync(Guid sensorId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.SensorId == sensorId).OrderByDescending(r => r.RecordedAt).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SensorReading>> GetLatestBatchAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet.OrderByDescending(r => r.RecordedAt).Take(limit).ToListAsync(cancellationToken);
    }
}

public class RoomOccupancyRepository : Repository<RoomOccupancy>, IRoomOccupancyRepository
{
    public RoomOccupancyRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<RoomOccupancy>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Include(o => o.Room).ThenInclude(r => r.Floor).Where(o => o.Room.Floor.BuildingId == buildingId).ToListAsync(cancellationToken);
    }

    public async Task<RoomOccupancy?> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(o => o.RoomId == roomId).OrderByDescending(o => o.RecordedAt).FirstOrDefaultAsync(cancellationToken);
    }
}

public class FloorPlanRepository : Repository<FloorPlan>, IFloorPlanRepository
{
    public FloorPlanRepository(AegisDbContext context) : base(context) { }

    public async Task<FloorPlan?> GetByFloorAsync(Guid floorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.FloorId == floorId, cancellationToken);
    }
}

public class EnergyReadingRepository : Repository<EnergyReading>, IEnergyReadingRepository
{
    public EnergyReadingRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<EnergyReading>> GetByBuildingAsync(Guid buildingId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(r => r.BuildingId == buildingId);
        if (from.HasValue) query = query.Where(r => r.ReadingDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.ReadingDate <= to.Value);
        return await query.OrderByDescending(r => r.ReadingDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EnergyReading>> GetByTypeAsync(EnergyReadingType type, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(r => r.ReadingType == type);
        if (from.HasValue) query = query.Where(r => r.ReadingDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.ReadingDate <= to.Value);
        return await query.OrderByDescending(r => r.ReadingDate).ToListAsync(cancellationToken);
    }
}
