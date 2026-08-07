using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.DigitalTwin;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/digital-twin")]
[Authorize]
public class DigitalTwinController : ControllerBase
{
    private readonly IDigitalTwinService _twinService;
    private readonly IIoTSensorRepository _sensorRepo;
    private readonly ISensorReadingRepository _readingRepo;
    private readonly IEnergyReadingRepository _energyRepo;
    private readonly ILogger<DigitalTwinController> _logger;

    public DigitalTwinController(
        IDigitalTwinService twinService, IIoTSensorRepository sensorRepo,
        ISensorReadingRepository readingRepo, IEnergyReadingRepository energyRepo,
        ILogger<DigitalTwinController> logger)
    {
        _twinService = twinService;
        _sensorRepo = sensorRepo;
        _readingRepo = readingRepo;
        _energyRepo = energyRepo;
        _logger = logger;
    }

    [HttpGet("campus/{campusId:guid}")]
    public async Task<IActionResult> GetCampusMap(Guid campusId, CancellationToken cancellationToken)
    {
        var map = await _twinService.GetCampusMapAsync(campusId, cancellationToken);
        return Ok(map);
    }

    [HttpGet("building/{buildingId:guid}")]
    public async Task<IActionResult> GetBuildingOverview(Guid buildingId, CancellationToken cancellationToken)
    {
        var overview = await _twinService.GetBuildingOverviewAsync(buildingId, cancellationToken);
        return Ok(overview);
    }

    [HttpPost("sensors")]
    public async Task<IActionResult> RegisterSensor([FromBody] SensorDto request, CancellationToken cancellationToken)
    {
        var sensor = new IoTSensor
        {
            Id = Guid.NewGuid(), Name = request.Name, HardwareId = request.HardwareId,
            SensorType = Enum.TryParse<SensorType>(request.SensorType, true, out var st) ? st : SensorType.Temperature,
            BuildingId = request.BuildingId, RoomId = request.RoomId
        };

        await _twinService.RegisterSensorAsync(sensor, cancellationToken);
        return Ok(new SensorDto { Id = sensor.Id, Name = sensor.Name, SensorType = sensor.SensorType.ToString() });
    }

    [HttpGet("sensors")]
    public async Task<IActionResult> ListSensors([FromQuery] Guid? buildingId, [FromQuery] string? type, CancellationToken cancellationToken)
    {
        IReadOnlyList<IoTSensor> sensors;
        if (buildingId.HasValue)
            sensors = await _sensorRepo.GetByBuildingAsync(buildingId.Value, cancellationToken);
        else if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<SensorType>(type, true, out var st))
            sensors = await _sensorRepo.GetByTypeAsync(st, cancellationToken);
        else
            sensors = await _sensorRepo.GetAllAsync(cancellationToken);

        var response = sensors.Select(s => new SensorDto
        {
            Id = s.Id, Name = s.Name, HardwareId = s.HardwareId,
            SensorType = s.SensorType.ToString(), Status = s.Status.ToString(),
            BuildingId = s.BuildingId, RoomId = s.RoomId, LastReadingAt = s.LastReadingAt
        });

        return Ok(new { total = response.Count(), sensors = response });
    }

    [HttpPost("sensors/ingest")]
    public async Task<IActionResult> IngestSensorData([FromBody] IngestSensorDataRequest request, CancellationToken cancellationToken)
    {
        var reading = await _twinService.IngestSensorDataAsync(request.SensorId, request.Value, request.Unit, cancellationToken);
        return Ok(new { reading.Id, reading.Value, reading.Unit, reading.RecordedAt });
    }

    [HttpGet("sensors/{sensorId:guid}/readings")]
    public async Task<IActionResult> GetSensorReadings(Guid sensorId, [FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var readings = await _readingRepo.GetBySensorAsync(sensorId, limit, cancellationToken);
        var response = readings.Select(r => new SensorReadingDto { Id = r.Id, SensorId = r.SensorId, Value = r.Value, Unit = r.Unit, RecordedAt = r.RecordedAt });
        return Ok(new { total = response.Count(), readings = response });
    }

    [HttpPost("occupancy")]
    public async Task<IActionResult> UpdateOccupancy(
        [FromQuery] Guid roomId, [FromQuery] string state, [FromQuery] int? occupants, [FromQuery] string? currentEvent, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OccupancyState>(state, true, out var s))
            return BadRequest(new { error = "invalid_state" });

        await _twinService.UpdateRoomOccupancyAsync(roomId, s, occupants, currentEvent, cancellationToken);
        return Ok(new { message = "Occupancy updated." });
    }

    [HttpPost("floor-plan/{floorId:guid}")]
    public async Task<IActionResult> UploadFloorPlan(Guid floorId, [FromBody] FloorPlanUploadDto request, CancellationToken cancellationToken)
    {
        var plan = await _twinService.UploadFloorPlanAsync(floorId, request.ImageUrl, request.RoomsLayout, request.ImageWidth, request.ImageHeight, cancellationToken);
        return Ok(new { plan.Id, plan.ImageUrl, plan.FloorId });
    }

    [HttpPost("energy")]
    public async Task<IActionResult> RecordEnergyReading([FromBody] EnergyReadingRequest request, CancellationToken cancellationToken)
    {
        var reading = new EnergyReading
        {
            Id = Guid.NewGuid(), BuildingId = request.BuildingId, SensorId = request.SensorId,
            ReadingType = Enum.TryParse<EnergyReadingType>(request.ReadingType, true, out var rt) ? rt : EnergyReadingType.Electricity,
            Consumption = request.Consumption, Unit = request.Unit, Cost = request.Cost
        };

        await _twinService.RecordEnergyReadingAsync(reading, cancellationToken);
        return Ok(new { reading.Id, reading.Consumption, reading.ReadingType, reading.ReadingDate });
    }

    [HttpGet("energy")]
    public async Task<IActionResult> GetEnergyReadings([FromQuery] Guid? buildingId, [FromQuery] string? type, CancellationToken cancellationToken)
    {
        IReadOnlyList<EnergyReading> readings;
        if (buildingId.HasValue)
            readings = await _energyRepo.GetByBuildingAsync(buildingId.Value, cancellationToken: cancellationToken);
        else if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<EnergyReadingType>(type, true, out var rt))
            readings = await _energyRepo.GetByTypeAsync(rt, cancellationToken: cancellationToken);
        else
            readings = await _energyRepo.GetAllAsync(cancellationToken);

        return Ok(new { total = readings.Count, readings });
    }
}

public class FloorPlanUploadDto
{
    public string ImageUrl { get; set; } = string.Empty;
    public List<RoomLayoutDto> RoomsLayout { get; set; } = new();
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
}

public class EnergyReadingRequest
{
    public Guid? BuildingId { get; set; }
    public Guid? SensorId { get; set; }
    public string ReadingType { get; set; } = "Electricity";
    public decimal Consumption { get; set; }
    public string? Unit { get; set; } = "kWh";
    public decimal? Cost { get; set; }
}
