using AegisCore.Domain.Entities;

namespace AegisCore.Application.DigitalTwin;

public interface IDigitalTwinService
{
    Task<CampusMapDto> GetCampusMapAsync(Guid campusId, CancellationToken cancellationToken = default);
    Task<BuildingOverviewDto> GetBuildingOverviewAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IoTSensor> RegisterSensorAsync(IoTSensor sensor, CancellationToken cancellationToken = default);
    Task<SensorReading> IngestSensorDataAsync(Guid sensorId, decimal value, string? unit, CancellationToken cancellationToken = default);
    Task UpdateRoomOccupancyAsync(Guid roomId, OccupancyState state, int? occupants, string? currentEvent, CancellationToken cancellationToken = default);
    Task<FloorPlan> UploadFloorPlanAsync(Guid floorId, string imageUrl, List<RoomLayoutDto> roomsLayout, int width, int height, CancellationToken cancellationToken = default);
    Task<EnergyReading> RecordEnergyReadingAsync(EnergyReading reading, CancellationToken cancellationToken = default);
}
