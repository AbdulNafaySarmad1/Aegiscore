using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.DigitalTwin;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.DigitalTwin;

public class DigitalTwinService : IDigitalTwinService
{
    private readonly IIoTSensorRepository _sensorRepo;
    private readonly ISensorReadingRepository _readingRepo;
    private readonly IRoomOccupancyRepository _occupancyRepo;
    private readonly IFloorPlanRepository _floorPlanRepo;
    private readonly IEnergyReadingRepository _energyRepo;
    private readonly ICampusRepository _campusRepo;
    private readonly IBuildingRepository _buildingRepo;
    private readonly IFloorRepository _floorRepo;
    private readonly IRoomRepository _roomRepo;
    private readonly ILogger<DigitalTwinService> _logger;

    public DigitalTwinService(
        IIoTSensorRepository sensorRepo, ISensorReadingRepository readingRepo,
        IRoomOccupancyRepository occupancyRepo, IFloorPlanRepository floorPlanRepo,
        IEnergyReadingRepository energyRepo, ICampusRepository campusRepo,
        IBuildingRepository buildingRepo, IFloorRepository floorRepo, IRoomRepository roomRepo,
        ILogger<DigitalTwinService> logger)
    {
        _sensorRepo = sensorRepo; _readingRepo = readingRepo; _occupancyRepo = occupancyRepo;
        _floorPlanRepo = floorPlanRepo; _energyRepo = energyRepo;
        _campusRepo = campusRepo; _buildingRepo = buildingRepo;
        _floorRepo = floorRepo; _roomRepo = roomRepo;
        _logger = logger;
    }

    public async Task<CampusMapDto> GetCampusMapAsync(Guid campusId, CancellationToken cancellationToken = default)
    {
        var campus = await _campusRepo.GetByIdAsync(campusId, cancellationToken)
            ?? throw new InvalidOperationException("Campus not found.");

        var buildings = await _buildingRepo.GetByCampusAsync(campusId, cancellationToken);
        var buildingMaps = new List<BuildingMapDto>();

        foreach (var building in buildings)
        {
            var floors = await _floorRepo.GetByBuildingAsync(building.Id, cancellationToken);
            var allRooms = new List<Room>();
            var floorMaps = new List<FloorMapDto>();

            foreach (var floor in floors)
            {
                var rooms = await _roomRepo.GetByFloorAsync(floor.Id, cancellationToken);
                allRooms.AddRange(rooms);
                var floorPlan = await _floorPlanRepo.GetByFloorAsync(floor.Id, cancellationToken);
                var layouts = DeserializeRoomLayouts(floorPlan?.RoomsLayoutJson);

                floorMaps.Add(new FloorMapDto
                {
                    Id = floor.Id, Name = floor.Name, FloorNumber = floor.FloorNumber,
                    FloorPlanUrl = floorPlan?.ImageUrl,
                    Rooms = rooms.Select(r =>
                    {
                        var layout = layouts.FirstOrDefault(l => l.RoomId == r.Id);
                        return new RoomMapDto
                        {
                            Id = r.Id, Name = r.Name, RoomNumber = r.RoomNumber, Capacity = r.Capacity,
                            X = layout?.X, Y = layout?.Y, Width = layout?.Width, Height = layout?.Height
                        };
                    }).ToList()
                });
            }

            var sensors = await _sensorRepo.GetByBuildingAsync(building.Id, cancellationToken);

            buildingMaps.Add(new BuildingMapDto
            {
                Id = building.Id, Name = building.Name, BuildingCode = building.BuildingCode,
                FloorCount = building.FloorCount, RoomCount = allRooms.Count,
                ActiveSensors = sensors.Count(s => s.Status == SensorStatus.Online),
                Floors = floorMaps
            });
        }

        return new CampusMapDto { CampusId = campusId, CampusName = campus.Name, BuildingCount = buildingMaps.Count, Buildings = buildingMaps };
    }

    public async Task<BuildingOverviewDto> GetBuildingOverviewAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        var building = await _buildingRepo.GetByIdAsync(buildingId, cancellationToken)
            ?? throw new InvalidOperationException("Building not found.");

        var sensors = await _sensorRepo.GetByBuildingAsync(buildingId, cancellationToken);
        var floors = await _floorRepo.GetByBuildingAsync(buildingId, cancellationToken);
        var allRooms = new List<Room>();
        foreach (var floor in floors) { allRooms.AddRange(await _roomRepo.GetByFloorAsync(floor.Id, cancellationToken)); }

        var energy = await _energyRepo.GetByBuildingAsync(buildingId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow, cancellationToken);

        return new BuildingOverviewDto
        {
            BuildingName = building.Name, TotalRooms = allRooms.Count, OccupiedRooms = 0, FreeRooms = allRooms.Count,
            SensorCount = sensors.Count, OnlineSensors = sensors.Count(s => s.Status == SensorStatus.Online),
            Sensors = sensors.Select(s => new SensorDto
            {
                Id = s.Id, Name = s.Name, SensorType = s.SensorType.ToString(), Status = s.Status.ToString(),
                BuildingId = s.BuildingId, RoomId = s.RoomId, LastValue = 0, LastReadingAt = s.LastReadingAt
            }).ToList(),
            Energy = energy.Count > 0 ? new EnergySummaryDto { TotalConsumptionKwh = energy.Sum(e => e.Consumption), Unit = "kWh" } : null
        };
    }

    public async Task<IoTSensor> RegisterSensorAsync(IoTSensor sensor, CancellationToken cancellationToken = default)
    {
        await _sensorRepo.AddAsync(sensor, cancellationToken);
        _logger.LogInformation("Sensor registered: {Name} ({Type})", sensor.Name, sensor.SensorType);
        return sensor;
    }

    public async Task<SensorReading> IngestSensorDataAsync(Guid sensorId, decimal value, string? unit, CancellationToken cancellationToken = default)
    {
        var sensor = await _sensorRepo.GetByIdAsync(sensorId, cancellationToken)
            ?? throw new InvalidOperationException("Sensor not found.");

        var reading = new SensorReading { Id = Guid.NewGuid(), SensorId = sensorId, Value = value, Unit = unit, TenantId = sensor.TenantId };
        await _readingRepo.AddAsync(reading, cancellationToken);

        sensor.LastReadingAt = DateTime.UtcNow;
        await _sensorRepo.UpdateAsync(sensor, cancellationToken);

        return reading;
    }

    public async Task UpdateRoomOccupancyAsync(Guid roomId, OccupancyState state, int? occupants, string? currentEvent, CancellationToken cancellationToken = default)
    {
        var occupancy = new RoomOccupancy
        {
            Id = Guid.NewGuid(), RoomId = roomId, State = state,
            CurrentOccupants = occupants, CurrentEvent = currentEvent, BookingUntil = state == OccupancyState.Reserved ? DateTime.UtcNow.AddHours(1) : null
        };

        await _occupancyRepo.AddAsync(occupancy, cancellationToken);
    }

    public async Task<FloorPlan> UploadFloorPlanAsync(Guid floorId, string imageUrl, List<RoomLayoutDto> roomsLayout, int width, int height, CancellationToken cancellationToken = default)
    {
        var existing = await _floorPlanRepo.GetByFloorAsync(floorId, cancellationToken);
        var floorPlan = existing ?? new FloorPlan { Id = Guid.NewGuid(), FloorId = floorId };

        floorPlan.ImageUrl = imageUrl;
        floorPlan.RoomsLayoutJson = JsonSerializer.Serialize(roomsLayout);
        floorPlan.ImageWidth = width;
        floorPlan.ImageHeight = height;

        if (existing == null) await _floorPlanRepo.AddAsync(floorPlan, cancellationToken);
        else await _floorPlanRepo.UpdateAsync(floorPlan, cancellationToken);

        return floorPlan;
    }

    public async Task<EnergyReading> RecordEnergyReadingAsync(EnergyReading reading, CancellationToken cancellationToken = default)
    {
        await _energyRepo.AddAsync(reading, cancellationToken);
        return reading;
    }

    private static List<RoomLayoutDto> DeserializeRoomLayouts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        return JsonSerializer.Deserialize<List<RoomLayoutDto>>(json) ?? new();
    }
}
