using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IIoTSensorRepository : IRepository<IoTSensor>
{
    Task<IReadOnlyList<IoTSensor>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IoTSensor>> GetByTypeAsync(SensorType type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IoTSensor>> GetByStatusAsync(SensorStatus status, CancellationToken cancellationToken = default);
}

public interface ISensorReadingRepository : IRepository<SensorReading>
{
    Task<IReadOnlyList<SensorReading>> GetBySensorAsync(Guid sensorId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SensorReading>> GetLatestBatchAsync(int limit = 50, CancellationToken cancellationToken = default);
}

public interface IRoomOccupancyRepository : IRepository<RoomOccupancy>
{
    Task<IReadOnlyList<RoomOccupancy>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<RoomOccupancy?> GetLatestByRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
}

public interface IFloorPlanRepository : IRepository<FloorPlan>
{
    Task<FloorPlan?> GetByFloorAsync(Guid floorId, CancellationToken cancellationToken = default);
}

public interface IEnergyReadingRepository : IRepository<EnergyReading>
{
    Task<IReadOnlyList<EnergyReading>> GetByBuildingAsync(Guid buildingId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnergyReading>> GetByTypeAsync(EnergyReadingType type, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
